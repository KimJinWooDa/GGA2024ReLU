using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Sentis;
using System.IO;
using System.Text;
using FF = Unity.Sentis.Functional;

public class RunPhi15 : MonoBehaviour
{
    // 모델을 위한 백엔드 타입 설정
    const BackendType backend = BackendType.GPUCompute;

    // 시작 문자열
    string outputString = "One day an alien came down from Mars. It saw a chicken";

    // 최대 토큰 수
    const int maxTokens = 100;

    // 무작위성을 조정하는 값, 값이 낮을수록 덜 무작위적
    const float predictability = 5f;

    // 텍스트 종료를 나타내는 특별한 토큰
    const int END_OF_TEXT = 50256;

    // 어휘를 저장할 배열
    string[] tokens;

    // Sentis 엔진 작업자
    IWorker engine;

    // 현재 생성된 토큰 인덱스
    int currentToken = 0;

    // 생성된 토큰을 저장할 배열
    int[] outputTokens = new int[maxTokens];

    // 생성된 총 토큰 수
    int totalTokens = 0;

    // 어휘병합 규칙과 어휘집을 저장할 배열 및 딕셔너리
    string[] merges;
    Dictionary<string, int> vocab;

    // 생성된 텍스트가 종료될 때 멈추도록 설정한 토큰 수
    const int stopAfter = 100;

    // 특수 문자 처리를 위한 배열
    int[] whiteSpaceCharacters = new int[256];
    int[] encodedCharacters = new int[256];

    void Start()
    {
        SetupWhiteSpaceShifts();
        LoadVocabulary();

        var model1 = ModelLoader.Load(Path.Join(Application.streamingAssetsPath, "phi15.sentis"));
        int outputIndex = model1.outputs.Count - 1;

        var model2 = FF.Compile(
            (input, currentToken) =>
            {
                var row = FF.Select(model1.Forward(input)[outputIndex], 1, currentToken);
                return FF.Multinomial(predictability * row, 1);
            },
            (model1.inputs[0], InputDef.Int(new TensorShape()))
        );

        engine = WorkerFactory.CreateWorker(backend, model2);
    }

    // 텍스트 생성을 처리하는 코루틴
    public IEnumerator GenerateText(string inputText, System.Action<string> callback)
    {
        DecodePrompt(inputText); // 입력된 텍스트를 프롬프트로 사용

        bool runInference = true;
        string generatedText = ""; // 이전 응답을 지우고 새로운 텍스트를 담기 위한 변수
        currentToken = 0;
        totalTokens = 0;

        while (runInference)
        {
            using var tokensSoFar = new TensorInt(new TensorShape(1, maxTokens), outputTokens);
            using var index = new TensorInt(currentToken);

            engine.Execute(new Dictionary<string, Tensor> { { "input_0", tokensSoFar }, { "input_1", index } });

            var probs = engine.PeekOutput() as TensorInt;
            probs.CompleteOperationsAndDownload();

            int ID = probs[0];

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
                generatedText = " ";  // 토큰이 범위를 벗어날 경우 공백 추가
            }
            else
            {
                generatedText = GetUnicodeText(tokens[ID]);  // 새로운 텍스트로 덮어씀
            }

            // 콜백을 통해 생성된 텍스트 반환
            callback?.Invoke(generatedText);

            yield return null;
        }

        Debug.Log("Text Generation Completed");
    }

    // 프롬프트를 디코딩하고 토큰으로 변환
    void DecodePrompt(string text)
    {
        var inputTokens = GetTokens(text);

        for (int i = 0; i < inputTokens.Count; i++)
        {
            outputTokens[i] = inputTokens[i];
        }
        currentToken = inputTokens.Count - 1;
    }

    // 어휘 데이터를 로드
    void LoadVocabulary()
    {
        var jsonText = File.ReadAllText(Path.Join(Application.streamingAssetsPath, "vocab_phi.json"));
        vocab = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonText);
        tokens = new string[vocab.Count];
        foreach (var item in vocab)
        {
            tokens[item.Value] = item.Key;
        }

        merges = File.ReadAllLines(Path.Join(Application.streamingAssetsPath, "merges.txt"));
    }

    // 유니코드 텍스트로 변환
    string GetUnicodeText(string text)
    {
        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(ShiftCharacterDown(text));
        return Encoding.UTF8.GetString(bytes);
    }

    // ASCII 텍스트로 변환
    string GetASCIIText(string newText)
    {
        var bytes = Encoding.UTF8.GetBytes(newText);
        return ShiftCharacterUp(Encoding.GetEncoding("ISO-8859-1").GetString(bytes));
    }

    // 특수 문자를 변환하기 위한 함수
    string ShiftCharacterDown(string text)
    {
        string outText = "";
        foreach (char letter in text)
        {
            outText += ((int)letter <= 256) ? letter : (char)whiteSpaceCharacters[(int)(letter - 256)];
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

    // 화이트스페이스 처리를 위한 설정
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

    // 화이트스페이스 여부 확인
    bool IsWhiteSpace(int i)
    {
        return i <= 32 || (i >= 127 && i <= 160) || i == 173;
    }

    // 텍스트를 토큰 리스트로 변환
    List<int> GetTokens(string text)
    {
        text = GetASCIIText(text);

        var inputTokens = new List<string>();
        foreach (var letter in text)
        {
            inputTokens.Add(letter.ToString());
        }

        ApplyMerges(inputTokens);

        var ids = new List<int>();
        foreach (var token in inputTokens)
        {
            if (vocab.TryGetValue(token, out int id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    // 어휘 병합 규칙 적용
    void ApplyMerges(List<string> inputTokens)
    {
        foreach (var merge in merges)
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

    // 엔진 해제
    private void OnDestroy()
    {
        engine?.Dispose();
    }
}
