---
license: gemma
language:
- ja
pipeline_tag: text-to-speech
tags:
- speech
- voice
- tts
- torchao
base_model:
- Aratako/Irodori-TTS-v4-Large
base_model_relation: quantized
---

# Irodori-TTS-v4-Large-Quantized

[![Code](https://img.shields.io/badge/Code-GitHub-black)](https://github.com/Aratako/Irodori-TTS) [![Base Model](https://img.shields.io/badge/Base%20Model-HuggingFace-yellow)](https://huggingface.co/Aratako/Irodori-TTS-v4-Large)

**Irodori-TTS-v4-Large-Quantized** provides post-training [torchao](https://github.com/pytorch/ao) quantized variants of [Aratako/Irodori-TTS-v4-Large](https://huggingface.co/Aratako/Irodori-TTS-v4-Large), a Japanese Text-to-Speech model supporting voice cloning, text-based Voice Design, style-controlled voice cloning, long reference audio, and emoji-based control.

## Quantization Variants

| Variant | Quantization | Checkpoint size | GPU support | Notes |
| :--- | :--- | ---: | :--- | :--- |
| `int8-weight-only` | W8A16 | 3,662 MiB | NVIDIA CUDA | INT8 weights with BF16 computation. Recommended as the general-purpose INT8 variant. |
| `int8-dynamic` | W8A8 | 3,662 MiB | NVIDIA CUDA | INT8 weights with dynamically quantized INT8 activations in compatible Linear layers. |
| `int4-weight-only` | W4A16, group size 128 | 2,818 MiB | NVIDIA Ampere or newer (compute capability 8.0+) | Uses the CUDA tinygemm INT4 kernel. This is the smallest variant. |
| `float8-weight-only` | FP8 weights, BF16 activations | 3,665 MiB | NVIDIA Ada, Hopper, or Blackwell (compute capability 8.9+) | Keeps activations in BF16 while storing compatible Linear weights in FP8. |
| `float8-dynamic` | FP8 weights and dynamic FP8 activations | 3,659 MiB | NVIDIA Ada, Hopper, or Blackwell (compute capability 8.9+) | Dynamically quantizes activations in compatible FP8 Linear layers. |

These checkpoints quantize compatible attention and MLP Linear layers in the shared text encoder, speaker encoder, and diffusion Transformer. Projectors, AdaLN, duration prediction, and other unsupported layers remain in BF16.

Runtime validation was performed with NVIDIA CUDA. CPU, ROCm, and Intel XPU execution depends on the available PyTorch and torchao kernels and has not been validated for this release. Use `--model-precision bf16` for inference.

## Usage

For installation instructions and complete CLI, Gradio, LoRA, and long-reference usage, see the [Irodori-TTS GitHub repository](https://github.com/Aratako/Irodori-TTS).

Install the CUDA 12.8 environment:

```bash
git clone https://github.com/Aratako/Irodori-TTS.git
cd Irodori-TTS
uv sync --extra cu128
```

Select a quantization variant by appending its subfolder name to the Hugging Face repo id:

```bash
uv run --no-sync python infer.py \
  --hf-checkpoint Aratako/Irodori-TTS-v4-Large-Quantized/int8-weight-only \
  --model-precision bf16 \
  --text "こんにちは、私はAIです。これは音声合成のテストです。" \
  --no-ref \
  --output-wav outputs/sample.wav
```

Replace `int8-weight-only` with any variant listed in the table above. Only the selected model variant and the shared tokenizer assets are downloaded.

## License & Ethical Restrictions

### License

This model is subject to the **[Gemma Terms of Use](https://ai.google.dev/gemma/terms)** because its shared text/caption encoder is derived from **[google/t5gemma-2-1b-1b](https://huggingface.co/google/t5gemma-2-1b-1b)**. Use and redistribution must comply with those terms, including the **[Gemma Prohibited Use Policy](https://ai.google.dev/gemma/prohibited_use_policy)**, as well as the additional ethical restrictions below.

### Ethical Restrictions

In addition to the license terms, the following ethical restrictions apply:

1. **No Impersonation:** Do not use this model to clone or impersonate the voice of any individual (e.g., voice actors, celebrities, public figures) without their explicit consent.
2. **No Misinformation:** Do not use this model to generate deepfakes or synthetic speech intended to mislead others or spread misinformation.
3. **Voice Generation Disclaimer:** When generating speech purely from text or captions without using reference audio, it is possible that the generated voice may coincidentally resemble that of a real person. This is strictly a probabilistic artifact within the latent space. The model was not trained with the intent of reproducing specific individuals.
4. **Liability Disclaimer:** The developers assume no liability for any misuse of this model. Users are solely responsible for ensuring their use of the generated content complies with applicable laws and regulations in their jurisdiction.
