#!/usr/bin/env python3
"""Build the PA1 report PDF: report.md -> report.html -> report.pdf (headless Chrome, A4).

Supports the small Markdown subset used in report.md: # / ## headings, paragraphs,
**bold**, *italic*, `code`. Figures are inserted after the section headings listed in FIGURES
(skipped if the image file does not exist). Usage: python3 docs/report/build_report.py
"""
import html
import os
import re
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "report.md")
OUT_HTML = os.path.join(HERE, "report.html")
OUT_PDF = os.path.join(HERE, "Report_PA1_BoomerangGuardian.pdf")
CHROME = os.path.expanduser("~/Applications/Google Chrome.app/Contents/MacOS/Google Chrome")

# (insert after this heading, image file, caption)
FIGURES = [
    ("1. Use of AI Tools", "screenshot-gameplay.png", "Gameplay: Halloween arena, HUD, minimap and hover trajectory preview."),
    ("2. Adoption of Scrum", "screenshot-of-pa1-sprint-board.png", "GitHub Project board mirroring the product backlog (issues per backlog item, one milestone per sprint)."),
]

CSS = """
@page { size: A4; margin: 13mm 14mm 12mm 14mm; }
body { font-family: -apple-system, "Helvetica Neue", Arial, sans-serif; font-size: 9.1pt; line-height: 1.32; color: #111; }
h1 { font-size: 14pt; margin: 0 0 2pt 0; }
.meta { font-size: 8.2pt; color: #555; margin: 0 0 6pt 0; }
h2 { font-size: 10.6pt; margin: 7pt 0 2pt 0; border-bottom: 1px solid #bbb; padding-bottom: 1pt; }
p { margin: 0 0 4pt 0; text-align: justify; }
code { font-family: Menlo, monospace; font-size: 8.2pt; background: #f2f2f2; padding: 0 2px; }
figure { margin: 3pt 0 5pt 0; text-align: center; }
figure img { max-width: 78%; max-height: 54mm; border: 1px solid #ccc; }
figcaption { font-size: 7.8pt; color: #555; margin-top: 1pt; }
.credits { font-size: 7.6pt; line-height: 1.25; }
.todo { background: #fff3c4; }
"""


def inline(text: str) -> str:
    t = html.escape(text, quote=False)
    t = re.sub(r"`([^`]+)`", r"<code>\1</code>", t)
    t = re.sub(r"\*\*([^*]+)\*\*", r"<b>\1</b>", t)
    t = re.sub(r"(?<!\*)\*([^*]+)\*(?!\*)", r"<i>\1</i>", t)
    return t


FIGURE_COUNT = [0]


def figure_html(name: str, caption: str) -> str:
    if not os.path.exists(os.path.join(HERE, name)):
        print(f"  (figure skipped, not found: {name})")
        return ""
    FIGURE_COUNT[0] += 1   # number only the figures actually shown
    return f'<figure><img src="{name}"><figcaption>Figure {FIGURE_COUNT[0]}. {html.escape(caption)}</figcaption></figure>'


def build_html() -> tuple[str, int]:
    blocks = [b.strip() for b in open(SRC, encoding="utf-8").read().split("\n\n") if b.strip()]
    parts, words = [], 0
    for b in blocks:
        if b.startswith("# "):
            parts.append(f"<h1>{inline(b[2:])}</h1>")
        elif b.startswith("## "):
            title = b[3:]
            parts.append(f"<h2>{inline(title)}</h2>")
            for after, img, cap in FIGURES:
                if title == after:
                    parts.append(figure_html(img, cap))
        elif b.startswith("Pu-Hsuan Wu") or "NYCU 535652" in b.split("\n")[0][:80]:
            parts.append(f'<p class="meta">{inline(b)}</p>')
        else:
            cls = []
            if b.startswith("**Asset credits.**"):
                cls.append("credits")
            if "⟦" in b:
                cls.append("todo")
            words += len(re.sub(r"[`*⟦⟧]", "", b).split())
            parts.append(f'<p class="{" ".join(cls)}">{inline(b)}</p>')
    body = "\n".join(parts)
    doc = f'<!doctype html><html><head><meta charset="utf-8"><style>{CSS}</style></head><body>{body}</body></html>'
    return doc, words


def main() -> int:
    doc, words = build_html()
    open(OUT_HTML, "w", encoding="utf-8").write(doc)
    if not os.path.exists(CHROME):
        print("Google Chrome not found; open report.html and print to PDF manually.")
        return 1
    subprocess.run([CHROME, "--headless=new", "--disable-gpu", "--no-pdf-header-footer",
                    f"--print-to-pdf={OUT_PDF}", f"file://{OUT_HTML}"],
                   check=True, capture_output=True)
    pages = len(re.findall(rb"/Type\s*/Page[^s]", open(OUT_PDF, "rb").read()))
    todo = doc.count("⟦")
    print(f"Built {os.path.basename(OUT_PDF)}: {pages} page(s), ~{words} words in body paragraphs"
          f"{f', {todo} placeholder(s) left' if todo else ''}.")
    if pages > 2:
        print("WARNING: more than 2 pages (requirement: 1-2 pages).")
    if words < 500:
        print("WARNING: fewer than 500 words.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
