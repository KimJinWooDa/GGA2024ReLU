using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Sentis;
using System.IO;
using System.Text;
using FF = Unity.Sentis.Functional;

/*
 *              Phi1.5 Inference Code
 *              ===========================
 *  
 *  Put this script on the Main Camera
 *  
 *  In Assets/StreamingAssets put:
 *  
 *  phi15.sentis (or put in asset folder)
 *  vocab.json
 *  merges.txt
 * 
 *  Install package com.unity.nuget.newtonsoft-json from packagemanger
 *  Install package com.unity.sentis
 * 
 */


public class RunPhi15: MonoBehaviour
{
    //Drop the tinystories.sentis or onnx file on here if using an asset:
    //public ModelAsset asset;
    const BackendType backend = BackendType.GPUCompute;

    //string outputString = "Once upon a time, there were three bears";
    string outputString = "One day an alien came down from Mars. It saw a chicken";

    // This is how many tokens you want. It can be adjusted.
    const int maxTokens = 100;

    //Make this smaller for more randomness
    const float predictability = 5f;

    //Special tokens
    const int END_OF_TEXT = 50256;

    //Store the vocabulary
    string[] tokens;

    IWorker engine;

    int currentToken = 0;
    int[] outputTokens = new int[maxTokens];

    // Used for special character decoding
    int[] whiteSpaceCharacters = new int[256];
    int[] encodedCharacters = new int[256];

    bool runInference = false;


    //stop after this many tokens
    const int stopAfter = 100;

    int totalTokens = 0;

    string[] merges;
    Dictionary<string, int> vocab;

    void Start()
    {
        SetupWhiteSpaceShifts();

        LoadVocabulary();

        var model1 = ModelLoader.Load(Path.Join(Application.streamingAssetsPath , "phi15.sentis"));

        int outputIndex = model1.outputs.Count - 1;
        //var model1 = ModelLoader.Load(asset);
        //Create a new model to select the random token:
        var model2 = FF.Compile(
            (input, currentToken) =>
            {
                var row = FF.Select(model1.Forward(input)[outputIndex], 1, currentToken);
                return FF.Multinomial(predictability * row, 1);
            },
            (model1.inputs[0], InputDef.Int(new TensorShape()))
        );

        engine = WorkerFactory.CreateWorker(backend, model2);

        DecodePrompt(outputString);

        runInference = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (runInference)
        {
            RunInference();
        }
    }

    void RunInference()
    {
        using var tokensSoFar = new TensorInt(new TensorShape(1, maxTokens), outputTokens);
        using var index = new TensorInt(currentToken);

        engine.Execute(new Dictionary<string, Tensor> { {"input_0", tokensSoFar },  { "input_1", index }});

        var probs = engine.PeekOutput() as TensorInt;
        //Debug.Log(probs.shape);

        probs.CompleteOperationsAndDownload();

        int ID = probs[0];

        //shift window down if got to the end
        if (currentToken >= maxTokens - 1)
        {
            for (int i = 0; i < maxTokens - 1; i++) outputTokens[i] = outputTokens[i + 1];
            currentToken--;
        }

        outputTokens[++currentToken] = ID;
        totalTokens++;

        if (ID == END_OF_TEXT || totalTokens >= stopAfter)
        {
            runInference = false;
        }
        else if (ID < 0 || ID >= tokens.Length)
        {
            // Really we should use the added_tokens.json for this
            outputString += " ";
        }
        else outputString += GetUnicodeText(tokens[ID]);

        Debug.Log(outputString);

    }

    void DecodePrompt(string text)
    {
        var inputTokens = GetTokens(text);

        for(int i = 0; i < inputTokens.Count; i++)
        {
            outputTokens[i] = inputTokens[i];
        }
        currentToken = inputTokens.Count - 1;
    }
   
    void LoadVocabulary()
    {
        var jsonText = File.ReadAllText(Path.Join(Application.streamingAssetsPath , "vocab.json"));
        vocab = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonText);
        tokens = new string[vocab.Count];
        foreach (var item in vocab)
        {
            tokens[item.Value] = item.Key;
        }

        merges = File.ReadAllLines(Path.Join(Application.streamingAssetsPath , "merges.txt"));
    }

    // Translates encoded special characters to Unicode
    string GetUnicodeText(string text)
    {
        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(ShiftCharacterDown(text));
        return Encoding.UTF8.GetString(bytes);
    }
    string GetASCIIText(string newText)
    {
        var bytes = Encoding.UTF8.GetBytes(newText);
        return ShiftCharacterUp(Encoding.GetEncoding("ISO-8859-1").GetString(bytes));
    }

    string ShiftCharacterDown(string text)
    {
        string outText = "";
        foreach (char letter in text)
        {
            outText += ((int)letter <= 256) ? letter :
                (char)whiteSpaceCharacters[(int)(letter - 256)];
        }
        return outText;
    }

    string ShiftCharacterUp(string text)
    {
        string outText = "";
        foreach (char letter in text)
        {
            outText += (char)encodedCharacters[(int)letter];
        }
        return outText;
    }

    void SetupWhiteSpaceShifts()
    {
        for (int i = 0, n = 0; i < 256; i++)
        {
            encodedCharacters[i] = i;
            if (IsWhiteSpace(i))
            {
                encodedCharacters[i] = n + 256;
                whiteSpaceCharacters[n++] = i;
            }
        }
    }

    bool IsWhiteSpace(int i)
    {
        //returns true if it is a whitespace character
        return i <= 32 || (i >= 127 && i <= 160) || i == 173;
    }

    List<int> GetTokens(string text)
    {
        text = GetASCIIText(text);

        // Start with a list of single characters
        var inputTokens = new List<string>();
        foreach(var letter in text)
        {
            inputTokens.Add(letter.ToString());
        }

        ApplyMerges(inputTokens);

        //Find the ids of the words in the vocab
        var ids = new List<int>();
        foreach(var token in inputTokens)
        {
            if (vocab.TryGetValue(token, out int id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    void ApplyMerges(List<string> inputTokens)
    {
        foreach(var merge in merges)
        {
            string[] pair = merge.Split(' ');
            int n = 0;
            while (n >= 0)
            {
                n = inputTokens.IndexOf(pair[0], n);
                if (n != -1 && n < inputTokens.Count - 1 && inputTokens[n + 1] == pair[1])
                {
                    inputTokens[n] += inputTokens[n + 1];
                    inputTokens.RemoveAt(n + 1);
                }
                if (n != -1) n++;
            }
        }
    }

    private void OnDestroy()
    {
        engine?.Dispose();
    }
    
}
