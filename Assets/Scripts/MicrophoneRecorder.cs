using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;

public class MicrophoneRecorder : MonoBehaviour
{
    public Action<string> transcriptionCompleteCallback;
    
    public AudioClip recordedClip;
    private const int maxSamples = 30 * 16000; // 30초 동안 16kHz 샘플링
    private float[] data;
    private int numSamples;

    // UI 버튼을 연결할 변수
    public Button startRecordingButton;
    public Button stopRecordingButton;

    // RunWhisper 클래스와 연결
    [SerializeField]
    private RunWhisper whisperEngine;

    [SerializeField]
    private TextMeshProUGUI transcriptionDisplay;
    [SerializeField] 
    private TextMeshProUGUI resultDisplay;

    // 오디오 파일 저장 경로
    private string saveFilePath;

    void Start()
    {
        // 버튼에 메서드 연결
        startRecordingButton.onClick.AddListener(StartRecording);
        stopRecordingButton.onClick.AddListener(StopRecording);

        // 처음에는 녹음 완료 버튼 비활성화
        stopRecordingButton.interactable = false;

        // 저장할 경로 설정
        saveFilePath = Path.Combine(Application.dataPath, "RecordedAudio.wav");
    }

    public void StartRecording()
    {
        // 녹음 시작
        recordedClip = Microphone.Start(null, false, 30, 16000);

        // 마이크로부터 녹음이 시작될 때까지 대기
        while (!(Microphone.GetPosition(null) > 0)) { }

        Debug.Log("Microphone recording started");

        // 녹음 완료 버튼 활성화, 녹음 시작 버튼 비활성화
        startRecordingButton.interactable = false;
        stopRecordingButton.interactable = true;
    }

    public void StopRecording()
    {
        if (Microphone.IsRecording(null))
        {
            // 녹음 완료
            Microphone.End(null);
            numSamples = recordedClip.samples;

            if (numSamples > maxSamples)
            {
                Debug.Log($"The AudioClip is too long. It must be less than 30 seconds. This clip is {numSamples / recordedClip.frequency} seconds.");
                return;
            }

            data = new float[maxSamples];
            numSamples = maxSamples;
            recordedClip.GetData(data, 0);

            Debug.Log("Microphone recording stopped");

            // 녹음 시작 버튼 활성화, 녹음 완료 버튼 비활성화
            startRecordingButton.interactable = true;
            stopRecordingButton.interactable = false;

            // 녹음 완료 후 오디오 파일로 저장
            SaveRecordedAudio(saveFilePath);

            // 녹음 완료 후 RunWhisper를 통해 텍스트 변환 시작
            ProcessRecordedAudio();
        }
    }

    public void SaveRecordedAudio(string filepath)
    {
        if (recordedClip != null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filepath));
            var filepathWithExtension = Path.ChangeExtension(filepath, ".wav");

            using (var fileStream = CreateEmptyWav(filepathWithExtension))
            {
                ConvertAndWriteWav(fileStream, recordedClip);
                WriteWavHeader(fileStream, recordedClip);
            }

            Debug.Log($"Audio saved at: {filepathWithExtension}");
        }
        else
        {
            Debug.LogError("No audio recorded to save.");
        }
    }

    private FileStream CreateEmptyWav(string filepath)
    {
        var fileStream = new FileStream(filepath, FileMode.Create);
        byte emptyByte = new byte();
        for (int i = 0; i < 44; i++) // WAV 헤더 공간
        {
            fileStream.WriteByte(emptyByte);
        }
        return fileStream;
    }

    private void ConvertAndWriteWav(FileStream fileStream, AudioClip clip)
    {
        var samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        var intData = new Int16[samples.Length];
        var bytesData = new Byte[samples.Length * 2];

        var rescaleFactor = 32767; // float을 Int16으로 변환

        for (var i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            var byteArray = BitConverter.GetBytes(intData[i]);
            byteArray.CopyTo(bytesData, i * 2);
        }

        fileStream.Write(bytesData, 0, bytesData.Length);
    }

    private void WriteWavHeader(FileStream fileStream, AudioClip clip)
    {
        var hz = clip.frequency;
        var channels = clip.channels;
        var samples = clip.samples;

        fileStream.Seek(0, SeekOrigin.Begin);

        fileStream.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"), 0, 4);
        fileStream.Write(BitConverter.GetBytes(fileStream.Length - 8), 0, 4);
        fileStream.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"), 0, 4);
        fileStream.Write(System.Text.Encoding.UTF8.GetBytes("fmt "), 0, 4);
        fileStream.Write(BitConverter.GetBytes(16), 0, 4);
        fileStream.Write(BitConverter.GetBytes((short)1), 0, 2);
        fileStream.Write(BitConverter.GetBytes((short)channels), 0, 2);
        fileStream.Write(BitConverter.GetBytes(hz), 0, 4);
        fileStream.Write(BitConverter.GetBytes(hz * channels * 2), 0, 4);
        fileStream.Write(BitConverter.GetBytes((short)(channels * 2)), 0, 2);
        fileStream.Write(BitConverter.GetBytes((short)16), 0, 2);
        fileStream.Write(System.Text.Encoding.UTF8.GetBytes("data"), 0, 4);
        fileStream.Write(BitConverter.GetBytes(samples * channels * 2), 0, 4);
    }

    // RunWhisper를 통해 녹음된 오디오 텍스트 변환 처리
    public void ProcessRecordedAudio()
    {
        if (whisperEngine != null && recordedClip != null)
        {
            // RunWhisper를 사용하여 트랜스크립션을 시작하고, 결과를 TextMeshPro에 표시
            whisperEngine.StartTranscription(recordedClip, UpdateTranscriptionDisplay);
        }
        else
        {
            Debug.LogError("RunWhisper 인스턴스나 녹음된 오디오 클립이 없습니다.");
        }
    }

    // 트랜스크립션 결과를 TextMeshPro에 업데이트하는 함수
    void UpdateTranscriptionDisplay(string transcription)
    {
        transcriptionCompleteCallback?.Invoke(transcription);
        if (transcriptionDisplay != null)
        {
            transcriptionDisplay.text = transcription;
        }
    }

    
}
