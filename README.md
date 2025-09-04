# ImageDuplicateAnalyzer

AI-powered image duplicate detection using CLIP neural network and ONNX Runtime.

[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ONNX Runtime](https://img.shields.io/badge/ONNX%20Runtime-Enabled-005CED?logo=onnx&logoColor=white)](https://onnxruntime.ai/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## What it does

Finds visually similar images in directories using deep learning. Unlike simple file hash comparison, this tool can detect:
- Resized versions of the same image
- Images with different compression
- Slightly modified copies

## How to run

### Prerequisites
- .NET 8 SDK or newer
- Windows/Linux/macOS

### Quick start
```bash
git clone https://github.com/eternally-voiceless/ImageDuplicateAnalyzer.git
cd ImageDuplicateAnalyzer
cd src/ImageDuplicateAnalyzer.Console
dotnet run
```

The app will:
1. Download the CLIP model on first run
2. Let you select source and target directories
3. Show similarity results in a table

## Technology

- **CLIP neural network** for image understanding
- **ONNX Runtime** for cross-platform AI inference  
- **Clean Architecture** with dependency injection
- **Spectre.Console** for interactive UI

## Project structure

```
src/
  ImageDuplicateAnalyzer.Console/    # Console app entry point
  ImageDuplicateAnalyzer.Core/       # Business logic and services
```

## Planned features

- WPF GUI with drag & drop support
- Unit tests for core components
- Performance optimizations for large collections
