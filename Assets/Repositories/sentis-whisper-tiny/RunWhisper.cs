using System.Collections;
using UnityEngine;
using Unity.Sentis;
using System.IO;
using Newtonsoft.Json;
using System.Text;
using System;
using System.Collections.Generic;

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

    // ?? ???? Start?? ??
    void Start()
    {
        SetupWhiteSpaceShifts();
        GetTokens();

        // ?? ???
        //@note: no default reload to avoid conflict with decisionsystem's reload 
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

                // ?????? ?? ? ?? ??
                transcriptionCompleteCallback?.Invoke(outputString);
            }
            else if (ID >= tokens.Length)
            {
                outputString += $"(time={(ID - START_TIME) * 0.02f})";
            }
            else
            {
                outputString += GetUnicodeText(tokens[ID]);
            }

            Debug.Log(outputString);
        }
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
