---
license: apache-2.0
library_name: unity-sentis
pipeline_tag: automatic-speech-recognition
---

# Whisper-Tiny model in Unity Sentis (Version 1.4.0-pre.3*)
(*Sentis files from 1.3.0 and earlier will not be compatible and would need to be recreated.)

This is the [Whisper Tiny](https://huggingface.co/openai/whisper-tiny) model tested to work in Unity 2023. It is a speech-to-text model. You feed in a 16kHz wav file and it outputs the best guess for what was said in the audio.

## How to Use
* Open a new scene in Unity 2023
* Import package ``com.unity.sentis`` version `1.4.0-pre.3` from the package manager.
* Put the `RunWhisper.cs` on the Main Camera
* Put the *.sentis files and the `vocab.json` in the Assets/StreamingAssets folder
* Add a 16kHz mono audio file up to 30 seconds long to your project and drag on to the audioClip field.
* **IMPORTANT:** The audio must be 16kHz. In the audio inspector select "Force Mono". And "Decompress on Load".
* You can add a step to convert 44kHz or 22kHz audio to 16kHz with [this model](https://huggingface.co/unity/sentis-audio-frequency-to-16khz)

When you press play the transcription of the audio will be displayed in the console window.

## Languages
The output starts with 4 tokens which you can set. One token specifies the input language. One token specifies whether it is straight transcription into the specified language or if it is translated to English. See [here](https://huggingface.co/openai/whisper-tiny) for more details.
These special tokens are defined in the added_tokens.json file.
