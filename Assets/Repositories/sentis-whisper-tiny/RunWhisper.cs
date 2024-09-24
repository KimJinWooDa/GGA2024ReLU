using System.Collections;
using UnityEngine;
using Unity.Sentis;
using System.IO;
using Newtonsoft.Json;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;

public class RunWhisper : MonoBehaviour
{
    IWorker decoderEngine, encoderEngine, spectroEngine;

    const BackendType backend = BackendType.GPUCompute;

    // ??? ??? ???? ????
    private AudioClip audioClip;

    // Maximum tokens for output
    const int maxTokens = 100;

    // Special tokens
    const int END_OF_TEXT = 50257;
    const int START_OF_TRANSCRIPT = 50258;
    const int KOREAN = 50264;
    const int TRANSCRIBE = 50359;
    const int NO_TIME_STAMPS = 50363;
    const int START_TIME = 50364;

    int numSamples;
    float[] data;
    string[] tokens;

    int currentToken = 0;
    int[] outputTokens = new int[maxTokens];

    // Special character decoding
    int[] whiteSpaceCharacters = new int[256];

    TensorFloat encodedAudio;

    bool transcribe = false;
    string outputString = "";

    // Maximum size of audioClip (30s at 16kHz)
    const int maxSamples = 30 * 16000;

    // ?????? ?? ??
    private Action<string> transcriptionCompleteCallback;
    
    private Dictionary<int, string> vocab;
    private List<byte> byteBuffer = new List<byte>();

    // ?? ???? Start?? ??
    void Start()
    {
        SetupWhiteSpaceShifts();
        GetTokens();

        // ?? ???
        //@note: no default reload to avoid conflict with decisionsystem's reload 

        string vocabPath = Application.streamingAssetsPath + "/multilingual.tiktoken";

        Dictionary<string, int> ranks = File.ReadLines(vocabPath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split())
            .ToDictionary(
                split => split[0], // Base64 
                split => int.Parse(split[1])
            );

        Dictionary<int, string> d = ranks.ToDictionary(
            kvp => kvp.Value,
            kvp => kvp.Key // Base64
        );
        
        //vocab dictionary to be saved and used in decoding elsewhere 
        vocab = d.ToDictionary(entry => entry.Key, entry => entry.Value);

        List<byte[]> decodedBytesList = new List<byte[]>();
        int[] values = { 45326, 120, 4815, 48267, 1517, 15933, 250, 119, 3049 }; // == ID 
        foreach (int v in values)
        {
            if (d.ContainsKey(v))
            {
                string base64String = d[v];
                try
                {
                    byte[] decodedBytes = Convert.FromBase64String(base64String);
                    decodedBytesList.Add(decodedBytes);
                }
                catch (FormatException ex)
                {
                    Debug.LogWarning($"Warning: Error decoding Base64 for value {v}: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"Warning: Value {v} not found in the dictionary.");
            }
        }

        byte[] resultBytes = decodedBytesList.SelectMany(b => b).ToArray();

        Debug.Log("Byte Array: " + BitConverter.ToString(resultBytes));
        try
        {
            string decodedString = System.Text.Encoding.UTF8.GetString(resultBytes);
            Debug.Log("Decoded String: " + decodedString);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error decoding bytes to UTF-8: " + ex.Message);
        }
    }

    public void ReloadModel(DecisionSystem.WhisperModel inModel)
    {
        decoderEngine?.Dispose();
        encoderEngine?.Dispose();
        spectroEngine?.Dispose();
        
        Model decoder = ModelLoader.Load(Application.streamingAssetsPath + "/AudioDecoder_Tiny.sentis"); //flag
        Model encoder = ModelLoader.Load(Application.streamingAssetsPath + "/AudioEncoder_Tiny.sentis"); //flag
        switch (inModel)
        {
            case DecisionSystem.WhisperModel.Tiny:
                decoder = ModelLoader.Load(Application.streamingAssetsPath + "/AudioDecoder_Tiny.sentis"); //flag
                encoder = ModelLoader.Load(Application.streamingAssetsPath + "/AudioEncoder_Tiny.sentis"); //flag
                break;
            case DecisionSystem.WhisperModel.Medium:
                decoder = ModelLoader.Load(Application.streamingAssetsPath + "/AudioDecoder_Medium.sentis"); //flag
                encoder = ModelLoader.Load(Application.streamingAssetsPath + "/AudioEncoder_Medium.sentis"); //flag
                break;
            case DecisionSystem.WhisperModel.Base:
                decoder = ModelLoader.Load(Application.streamingAssetsPath + "/AudioDecoder_Base.sentis"); //flag
                encoder = ModelLoader.Load(Application.streamingAssetsPath + "/AudioEncoder_Base.sentis"); //flag
                break;
        }
        Model decoderWithArgMax = Functional.Compile(
            (tokens, audio) => Functional.ArgMax(decoder.Forward(tokens, audio)[0], 2),
            (decoder.inputs[0], decoder.inputs[1])
        );
        Model spectro = ModelLoader.Load(Application.streamingAssetsPath + "/LogMelSepctro.sentis");

        // ?? ???
        decoderEngine = WorkerFactory.CreateWorker(backend, decoderWithArgMax);
        encoderEngine = WorkerFactory.CreateWorker(backend, encoder);
        spectroEngine = WorkerFactory.CreateWorker(backend, spectro);

    }

    // ???? ??? ??? ?? ?????? ??
    public void StartTranscription(AudioClip clip, Action<string> callback = null)
    {
        audioClip = clip;
        transcriptionCompleteCallback = callback;

        // ?????? ?? ???
        outputTokens[0] = START_OF_TRANSCRIPT;
        outputTokens[1] = KOREAN;
        outputTokens[2] = TRANSCRIBE;
        outputTokens[3] = NO_TIME_STAMPS;
        currentToken = 3;
        outputString = "";

        // ??? ?? ? ???
        LoadAudio();
        EncodeAudio();

        // ?????? ??
        transcribe = true;
    }

    // ??? ??? ??
    void LoadAudio()
    {
        if (audioClip.frequency != 16000)
        {
            Debug.Log($"The audio clip should have frequency 16kHz. It has frequency {audioClip.frequency / 1000f}kHz");
            return;
        }

        numSamples = audioClip.samples;

        if (numSamples > maxSamples)
        {
            Debug.Log($"The AudioClip is too long. It must be less than 30 seconds. This clip is {numSamples / audioClip.frequency} seconds.");
            return;
        }

        data = new float[maxSamples];
        numSamples = maxSamples;
        audioClip.GetData(data, 0);
    }

    // ??? ???
    void EncodeAudio()
    {
        using var input = new TensorFloat(new TensorShape(1, numSamples), data);

        spectroEngine.Execute(input);
        var spectroOutput = spectroEngine.PeekOutput() as TensorFloat;

        encoderEngine.Execute(spectroOutput);
        encodedAudio = encoderEngine.PeekOutput() as TensorFloat;
    }

    // Update ????? ????? ?????? ??
    void Update()
    {
        if (transcribe && currentToken < outputTokens.Length - 1)
        {
            using var tokensSoFar = new TensorInt(new TensorShape(1, outputTokens.Length), outputTokens);

            var inputs = new Dictionary<string, Tensor>
            {
                {"input_0", tokensSoFar },
                {"input_1", encodedAudio }
            };

            decoderEngine.Execute(inputs);
            var tokensPredictions = decoderEngine.PeekOutput() as TensorInt;

            tokensPredictions.CompleteOperationsAndDownload();

            int ID = tokensPredictions[currentToken];

            outputTokens[++currentToken] = ID;

            if (ID == END_OF_TEXT)
            {
                transcribe = false;
                transcriptionCompleteCallback?.Invoke(outputString);
            }
            else if (ID >= tokens.Length)
            {
                Debug.LogWarning($"Token ID {ID} is larger than {tokens.Length}");
                outputString += $"(time={(ID - START_TIME) * 0.02f})";
            }
            else
            {
                Debug.Log($"token[ID]={tokens[ID]}");
                //outputString += GetUnicodeText(tokens[ID]);
                outputString += DecodeID(ID);
            }

            Debug.Log(outputString);
            Debug.Log(string.Join(", ", outputTokens));
        }
    }

    private string DecodeID(int id)
    {
        byte[] decodedBytes = new byte[] { };
        if (vocab.ContainsKey(id))
        {
            string base64String = vocab[id];
            try
            {
                decodedBytes = Convert.FromBase64String(base64String);
            }
            catch (FormatException ex)
            {
                Debug.LogWarning($"Warning: Error decoding Base64 for value {id}: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"Warning: Value {id} not found in the dictionary.");
        }

        Debug.Log("Byte Array: " + BitConverter.ToString(decodedBytes));
        // string decodedString = string.Empty;
        // try
        // {
        //     decodedString = System.Text.Encoding.UTF8.GetString(decodedBytes);
        //     Debug.Log("Decoded String: " + decodedString);
        // }
        // catch (Exception ex)
        // {
        //     Debug.LogError("Error decoding bytes to UTF-8: " + ex.Message);
        // }
        string decodedString = ProcessDecodedBytes(decodedBytes);

        return decodedString;
    }

    private string ProcessDecodedBytes(byte[] decodedBytes)
    {
        string result = string.Empty;

        foreach (byte b in decodedBytes)
        {
            byteBuffer.Add(b);
            if (IsCompleteUTF8Sequence(byteBuffer))
            {
                try
                {
                    result += Encoding.UTF8.GetString(byteBuffer.ToArray());
                    byteBuffer.Clear();
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error decoding bytes to UTF-8: " + ex.Message);
                    byteBuffer.Clear();
                }
            }
        }

        return result;
    }
    
    private bool IsCompleteUTF8Sequence(List<byte> byteList)
    {
        byte firstByte = byteList[0];

        // Single byte (ASCII)
        if (firstByte <= 0x7F) return true;

        // Two-byte sequence (110xxxxx 10xxxxxx)
        if (firstByte >= 0xC2 && firstByte <= 0xDF && byteList.Count >= 2)
        {
            return (byteList[1] & 0xC0) == 0x80; // Check if the second byte starts with 10xxxxxx
        }

        // Three-byte sequence (1110xxxx 10xxxxxx 10xxxxxx)
        if (firstByte >= 0xE0 && firstByte <= 0xEF && byteList.Count >= 3)
        {
            return (byteList[1] & 0xC0) == 0x80 && (byteList[2] & 0xC0) == 0x80;
        }

        // Four-byte sequence (11110xxx 10xxxxxx 10xxxxxx 10xxxxxx)
        if (firstByte >= 0xF0 && firstByte <= 0xF7 && byteList.Count >= 4)
        {
            return (byteList[1] & 0xC0) == 0x80 && (byteList[2] & 0xC0) == 0x80 && (byteList[3] & 0xC0) == 0x80;
        }

        // If not yet complete, return false
        return false;
    }

    // ?? ??? ????? ??
    string GetUnicodeText(string text)
    {
        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(ShiftCharacterDown(text));
        return Encoding.UTF8.GetString(bytes);
    }

    string ShiftCharacterDown(string text)
    {
        string outText = "";
        foreach (char letter in text)
        {
            outText += ((int)letter <= 256) ? letter :
                (char)whiteSpaceCharacters[(int)(letter - 256)];
        }
        Debug.Log("outText:" + string.Join(", ", outText));
        return outText;
    }

    void SetupWhiteSpaceShifts()
    {
        for (int i = 0, n = 0; i < 256; i++)
        {
            if (IsWhiteSpace((char)i)) whiteSpaceCharacters[n++] = i;
        }
    }

    bool IsWhiteSpace(char c)
    {
        return !(('!' <= c && c <= '~') || ('?' <= c && c <= '?') || ('?' <= c && c <= '?'));
    }

    // vocab.json?? ?? ????
    void GetTokens()
    {
        string vocabFilePath = Path.Combine(Application.streamingAssetsPath, "vocab.json");

        if (!File.Exists(vocabFilePath))
        {
            Debug.LogError("vocab.json file not found at: " + vocabFilePath);
            return;
        }

        var jsonText = File.ReadAllText(vocabFilePath);
        var vocab = JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonText);

        tokens = new string[vocab.Count];
        foreach (var item in vocab)
        {
            tokens[item.Value] = item.Key;
        }

        Debug.Log("Tokens loaded successfully.");
    }

    private void OnDestroy()
    {
        decoderEngine?.Dispose();
        encoderEngine?.Dispose();
        spectroEngine?.Dispose();
    }
}
