#!/usr/bin/env python3
"""Regenere Client/wwwroot/docs/presentation.pdf a partir de Docs/Presentation.md."""
import base64
import json
import mimetypes
import re
import subprocess
import sys
from pathlib import Path

import markdown

REPO_ROOT = Path(__file__).resolve().parents[2]
SOURCE_MD = REPO_ROOT / "Docs" / "Presentation.md"
OUTPUT_PDF = REPO_ROOT / "Client" / "wwwroot" / "docs" / "presentation.pdf"
TMP_HTML = REPO_ROOT / ".claude" / "hooks" / "_presentation_tmp.html"

CSS = """
@page { size: A4 landscape; margin: 1.5cm; }
body { font-family: 'Segoe UI', Arial, sans-serif; color: #1a1a1a; line-height: 1.5; }
h1, h2, h3 { color: #0b3d91; }
h1 { border-bottom: 2px solid #0b3d91; padding-bottom: 0.2em; }
img {
  max-width: 100%; max-height: 17cm; width: auto; height: auto;
  display: block; margin: 0.5em auto;
  page-break-before: always; page-break-after: always; page-break-inside: avoid;
}
h1, h2, h3 { page-break-after: avoid; }
code { background: #f4f4f4; border-radius: 4px; padding: 0.1em 0.3em; font-family: Consolas, monospace; }
pre {
  background: #f4f4f4; border-radius: 4px; padding: 1em;
  font-family: Consolas, monospace; font-size: 11px; line-height: 1.35;
  white-space: pre-wrap; word-break: break-word; overflow-wrap: anywhere;
  page-break-inside: avoid;
}
pre code { background: none; padding: 0; }
table { border-collapse: collapse; width: 100%; page-break-inside: avoid; }
th, td { border: 1px solid #ccc; padding: 0.4em 0.6em; }
"""

IMG_SRC_RE = re.compile(r'(<img [^>]*src=")([^"]+)(")')


def inline_local_images(html: str, base_dir: Path) -> str:
    """Remplace les src d'images relatives par des data URI base64 pour que Chrome
    puisse les afficher quel que soit l'emplacement du HTML temporaire."""

    def replace(match: re.Match) -> str:
        prefix, src, suffix = match.groups()
        if src.startswith(("http://", "https://", "data:")):
            return match.group(0)
        image_path = (base_dir / src).resolve()
        if not image_path.exists():
            print(f"Image introuvable, ignorée: {image_path}", file=sys.stderr)
            return match.group(0)
        mime, _ = mimetypes.guess_type(str(image_path))
        mime = mime or "application/octet-stream"
        data = base64.b64encode(image_path.read_bytes()).decode("ascii")
        return f'{prefix}data:{mime};base64,{data}{suffix}'

    return IMG_SRC_RE.sub(replace, html)


def find_browser() -> str:
    candidates = [
        r"C:\Program Files\Google\Chrome\Application\chrome.exe",
        r"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
        r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        r"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
    ]
    for path in candidates:
        if Path(path).exists():
            return path
    raise FileNotFoundError("Aucun navigateur Chrome/Edge trouve pour generer le PDF.")


def hook_targets_presentation() -> bool:
    """Quand invoque comme hook PostToolUse, lit le JSON stdin et verifie que le fichier
    modifie est bien Docs/Presentation.md. Renvoie True si l'appel n'est pas un hook
    (exécution manuelle) ou si le fichier correspond."""
    if sys.stdin.isatty():
        return True
    raw = sys.stdin.read()
    if not raw.strip():
        return True
    try:
        payload = json.loads(raw)
    except json.JSONDecodeError:
        return True
    file_path = (payload.get("tool_input") or {}).get("file_path") or (
        payload.get("tool_response") or {}
    ).get("filePath")
    if not file_path:
        return True
    normalized = file_path.replace("\\", "/")
    return normalized.endswith("Docs/Presentation.md")


def main() -> int:
    if not hook_targets_presentation():
        return 0

    if not SOURCE_MD.exists():
        print(f"Source introuvable: {SOURCE_MD}", file=sys.stderr)
        return 1

    md_text = SOURCE_MD.read_text(encoding="utf-8")
    body_html = markdown.markdown(md_text, extensions=["tables", "fenced_code"])
    body_html = inline_local_images(body_html, SOURCE_MD.parent)
    html = f"<!doctype html><html><head><meta charset='utf-8'><style>{CSS}</style></head><body>{body_html}</body></html>"
    TMP_HTML.write_text(html, encoding="utf-8")

    OUTPUT_PDF.parent.mkdir(parents=True, exist_ok=True)

    browser = find_browser()
    cmd = [
        browser,
        "--headless=new",
        "--disable-gpu",
        f"--print-to-pdf={OUTPUT_PDF}",
        "--no-pdf-header-footer",
        TMP_HTML.as_uri(),
    ]
    result = subprocess.run(cmd, capture_output=True, text=True, timeout=60)

    TMP_HTML.unlink(missing_ok=True)

    if result.returncode != 0 or not OUTPUT_PDF.exists():
        print("Echec generation PDF:", result.stderr, file=sys.stderr)
        return 1

    print(f"presentation.pdf regenere ({OUTPUT_PDF})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
