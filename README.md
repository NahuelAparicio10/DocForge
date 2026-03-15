# DocForge

<p align="center">
  <img src="https://img.shields.io/badge/Windows-Desktop%20App-0078D6?style=for-the-badge&logo=windows&logoColor=white" />
  <img src="https://img.shields.io/badge/.NET-8-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
  <img src="https://img.shields.io/badge/WPF-UI-0C54C2?style=for-the-badge" />
  <img src="https://img.shields.io/badge/Ollama-Local%20AI-black?style=for-the-badge" />
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" />
</p>

<h3 align="center">PDF text extraction, paragraph reconstruction, local AI summaries, and clean TXT / DOCX export.</h3>

<p align="center">
  Built for a practical desktop workflow: simple, local, fast, and batch-friendly.
</p>

## Features

### PDF Processing
- Extract embedded text from PDF files
- Drag & drop support for one or many PDFs
- Batch processing workflow for multiple documents at once
- Independent per-document state for extracted text and AI summary
- Automatic output naming based on the original PDF file

### Text Reconstruction
- Rebuilds paragraph structure after extraction
- Improves spacing and readability compared to raw extracted text
- Better suited for exporting to clean TXT or DOCX files

### Local AI Summary
- Optional local AI summaries powered by **Ollama**
- AI summary actions are automatically disabled if Ollama is not installed or running
- No paid cloud API required
- Works locally and is privacy-friendly

### Export
- Export extracted text to:
  - `.txt`
  - `.docx`
- Export AI summaries to:
  - `.txt`
  - `.docx`
- Export the selected document only or all loaded documents at once
- Output files are saved next to the original PDF

## Export Naming

DocForge automatically saves exported files next to the source PDF using the following format:

### Extracted text
- `MyDocument.txt`
- `MyDocument.docx`

### AI summary
- `MyDocument_ai_summary.txt`
- `MyDocument_ai_summary.docx`

## Architecture

The project follows a clean MVVM-oriented structure with service abstractions for extraction, reconstruction, export, and summarization.

```text
DocForge/
├── App/
│   ├── Views/                  # WPF XAML views
│   ├── ViewModels/             # UI state, commands, multi-document workflow
│   ├── Styles/                 # shared theme resources and button styles
│   └── App.xaml.cs             # app bootstrap and dependency injection
│
├── Application/
│   └── Abstractions/           # interfaces for extraction, export, reconstruction, summary
│
├── Infrastructure/
│   └── Services/               # concrete implementations
│       ├── PdfPigTextExtractor.cs
│       ├── TextStructureReconstructor.cs
│       ├── TxtExportService.cs
│       ├── DocxExportService.cs
│       └── OllamaSummaryService.cs
```
## Key design decisions

- **MVVM-based UI** for a clean separation of concerns
- **Service abstractions** to keep extraction, export, reconstruction, and summarization decoupled
- **Optional AI integration** so the app remains fully usable without Ollama
- **Batch-first workflow** for handling many PDFs in one session
- **Automatic per-file export naming** for a practical desktop workflow
- **Improved DOCX export** using paragraph blocks instead of raw line-by-line dumping

## Getting Started

### Requirements

- Windows 10 / 11
- .NET 8 SDK

### Run from source

```bash
git clone https://github.com/NahuelAparicio10/DocForge.git
cd DocForge
dotnet run --project src/DocForge.App/DocForge.App.csproj
```
## AI Summary Setup

AI summaries in DocForge are powered by Ollama, running locally on the user's machine.

### 1. Install Ollama

Install Ollama for Windows from its official site.

### 2. Pull a model

For example:

```powershell
ollama pull llama3.2:3b
```
### 3. Verify that Ollama is running
```
Invoke-RestMethod -Method Get -Uri "http://localhost:11434/api/tags"
```
If DocForge detects Ollama correctly, AI summary actions will be enabled automatically.

## Usage

| Step | Action |
|------|--------|
| 1 | Add PDFs using the file picker or drag & drop |
| 2 | Click **Extract All** to process all loaded documents |
| 3 | Select a document from the list to preview its extracted text |
| 4 | Optionally generate an AI summary for the selected document or for all documents |
| 5 | Export extracted text or AI summaries as `.txt` or `.docx` |
| 6 | Find the exported files next to the original PDFs |

## Example Workflow

### Batch extraction

Load these files into the app:

- `CombatDesign.pdf`
- `LevelFlow.pdf`
- `AIResearch.pdf`

After extraction, DocForge can export:

- `CombatDesign.docx`
- `LevelFlow.docx`
- `AIResearch.docx`

### AI summary output

If Ollama is available, DocForge can also generate:

- `CombatDesign_ai_summary.docx`
- `LevelFlow_ai_summary.docx`
- `AIResearch_ai_summary.docx`

## Why local AI?

DocForge uses local AI summarization through Ollama to keep the workflow:

- free to use
- offline-capable
- privacy-friendly
- independent from paid cloud APIs

This means the app remains useful without AI, while providing a stronger workflow for users who want local summaries.

## Current Limitations

- AI summaries require Ollama running locally
- PDF extraction quality depends on the presence of embedded selectable text
- Scanned PDFs are not ideal unless OCR is added in the future
- Very large documents may benefit from chunk-based summarization in future versions

## Roadmap

Possible future improvements:

- OCR support for scanned PDFs
- Smarter heading detection
- Section-aware summarization
- Export to a custom output folder
- Progress indicators per document
- Richer metadata extraction

## Tech Stack

- C#
- .NET 8
- WPF
- CommunityToolkit.Mvvm
- OpenXML SDK
- PdfPig
- Ollama

## License

This project is licensed under the MIT License.

## Author

**Nahuel Aparicio**  
Gameplay & Systems Programmer
