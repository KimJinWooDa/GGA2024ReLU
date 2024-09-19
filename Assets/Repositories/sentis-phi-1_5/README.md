---
license: mit
library_name: unity-sentis
pipeline_tag: text-generation
---


# Phi 1.5 Model in Unity Sentis (Version 1.5.0-pre.2)
*Version 1.3.0 Sentis files are not compatible with Sentis 1.5.0 and need to be recreated/downloaded

This is the [Microsoft Phi 1.5](https://huggingface.co/microsoft/phi-1_5) model checked to run on Unity 2023. Phi 1.5 is a Large Language Model that was trained on synthesized data. Please see their page for more information about the model and license.
The model has 1.3 billion parameters.


## How to Use
* Create a new scene in Unity 2023
* Install `com.unity.sentis` version `1.5.0-pre.2` and `com.unity.nuget.newtonsoft-json` packages
* Add the RunPhi15.cs file to the Main Camera
* Put `phi15.sentis`, `vocab.json` and `merges.txt` in the Assets/StreamingAssets folder
* Adjust some of the variables such as the `outputText` string to set the prompt
* Press run
* The output will appear in the console window (after some time. You may want to open the RAM inspector to see what is going on.)

## Information
This is the float32 version so it requires a lot of RAM (16GB) and VRAM (8GB). With less RAM it may take a long time to load. In the future we may add the float16 or uint8 quantized versions. This version also doesn't use caching of previous iterations so it is not the optimum speed but the implementation is simpler.

## Example Input
```
Once upon a time, there were three
```
## Example Output
```
Once upon a time, there were three friends named Alice, Bob, and Carol. They were all passionate about mathematics and loved solving complex problems together. One day, they came across a challenging problem that required them to find the area of a triangle using the Pythagorean theorem.

Alice, being the most experienced in geometry, took the lead and explained the steps to her friends. "To find the area of a triangle, we need to multiply the base by the height and divide the result by 2," she said confidently.

Bob, who was always curious, asked, "But why do we divide the result by 2? Can't we just multiply the base and height?"

Alice smiled and replied, "That's a great question, Bob. We divide by 2 because the area of a triangle is half of the product of its base and height. It's a fundamental concept in geometry."

Carol, who had been listening intently, added, "So, if we have a triangle with...
```

## Unity Sentis
Unity Sentis is the inference engine which runs on Unity 2023. More can be found about it [here](https://unity.com/products/sentis)

## Disclaimer
Like any LLM, this model has the possibility to generate undesirable or untruthful text. Use at your discretion.