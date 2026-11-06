# Сборка отчётов .docx из Markdown с титульным листом из шаблона (Лаб4_ХузинНИ_231-338.docx)
import docx, glob, re, os, sys, copy
from docx.shared import Pt, Cm
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml.ns import qn
from docx.oxml import OxmlElement

ROOT = os.path.dirname(os.path.abspath(__file__))
TEMPLATE = glob.glob(os.path.expanduser('~/Downloads/*4_*docx'))[0]
DISC = 'Технологии распространения игрового контента'
FONT = 'Times New Roman'

def set_text(p, text):
    runs = p.runs
    runs[0].text = text
    for r in runs[1:]:
        r._r.getparent().remove(r._r)

def fmt_run(r, size=14, bold=False, italic=False, mono=False):
    r.font.name = 'Courier New' if mono else FONT
    r._r.get_or_add_rPr().rFonts.set(qn('w:eastAsia'), r.font.name)
    r.font.size = Pt(size); r.bold = bold; r.italic = italic

def add_runs(p, text, size=14, bold=False):
    # **жирный** и `код`
    for part in re.split(r'(\*\*[^*]+\*\*|`[^`]+`)', text):
        if not part: continue
        if part.startswith('**'): fmt_run(p.add_run(part[2:-2]), size, True)
        elif part.startswith('`'): fmt_run(p.add_run(part[1:-1]), size - 2, bold, mono=True)
        else: fmt_run(p.add_run(part), size, bold)

def para(d, text, size=14, bold=False, first=True, align=WD_ALIGN_PARAGRAPH.JUSTIFY, before=0, after=3, left=None, hang=None):
    p = d.add_paragraph()
    pf = p.paragraph_format
    pf.alignment = align; pf.space_before = Pt(before); pf.space_after = Pt(after); pf.line_spacing = 1.5
    if first: pf.first_line_indent = Cm(1.25)
    if left is not None: pf.left_indent = Cm(left)
    if hang is not None: pf.first_line_indent = Cm(-hang)
    add_runs(p, text, size, bold)
    return p

def borders(tbl):
    tblPr = tbl._tbl.tblPr
    b = OxmlElement('w:tblBorders')
    for e in ('top', 'left', 'bottom', 'right', 'insideH', 'insideV'):
        x = OxmlElement('w:' + e)
        x.set(qn('w:val'), 'single'); x.set(qn('w:sz'), '4'); x.set(qn('w:color'), '000000')
        b.append(x)
    tblPr.append(b)

def table(d, rows):
    n = max(len(r) for r in rows)
    t = d.add_table(rows=len(rows), cols=n)
    borders(t)
    for i, r in enumerate(rows):
        for j in range(n):
            c = t.cell(i, j); c.text = ''
            p = c.paragraphs[0]; p.paragraph_format.space_after = Pt(2)
            add_runs(p, r[j] if j < len(r) else '', 11, bold=(i == 0))
    d.add_paragraph().paragraph_format.space_after = Pt(4)

def build(md_path, out_path, number):
    d = docx.Document(TEMPLATE)
    ps = d.paragraphs
    set_text(ps[16], 'Практическая работа № %s' % number)
    set_text(ps[18], 'Дисциплина: ' + DISC)
    body = d.element.body
    # удалить всё после титула (параграф 37 содержит sectPr)
    keep_until = ps[37]._p
    el = keep_until.getnext()
    while el is not None and el.tag != qn('w:sectPr'):
        nxt = el.getnext(); body.remove(el); el = nxt
    lines = open(md_path, encoding='utf-8').read().splitlines()
    i = 0; first_h1 = True; table_rows = []
    def flush():
        nonlocal table_rows
        if table_rows: table(d, table_rows); table_rows = []
    while i < len(lines):
        ln = lines[i].rstrip(); i += 1
        if ln.startswith('|'):
            if re.match(r'^\|[\s\-:|]+\|?$', ln): continue
            table_rows.append([c.strip() for c in ln.strip().strip('|').split('|')]); continue
        flush()
        if not ln.strip() or ln.strip() == '---': continue
        m = re.match(r'!\[(.*?)\]\((.*?)\)', ln)
        if m:
            path = os.path.join(ROOT, m.group(2))
            if os.path.exists(path):
                p = d.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
                p.add_run().add_picture(path, width=Cm(13))
                para(d, m.group(1), 12, first=False, align=WD_ALIGN_PARAGRAPH.CENTER)
            continue
        if ln.startswith('# '):
            continue
        if ln.startswith('## '):
            para(d, ln[3:], 15, True, first=False, align=WD_ALIGN_PARAGRAPH.LEFT, before=12, after=6); continue
        if ln.startswith('### '):
            para(d, ln[4:], 14, True, first=False, align=WD_ALIGN_PARAGRAPH.LEFT, before=9, after=4); continue
        if ln.startswith('> '):
            para(d, ln[2:], 13, first=False, left=1.0); continue
        mb = re.match(r'^\s*(?:[-•*]|\d+\.|- \[[ x]\])\s+(.*)', ln)
        if mb:
            txt = ln.strip()
            txt = re.sub(r'^- \[x\]', '☑', txt); txt = re.sub(r'^- \[ \]', '☐', txt)
            txt = re.sub(r'^[-*]\s+', '– ', txt)
            para(d, txt, 14, first=False, left=1.25, hang=0.6, after=2); continue
        para(d, ln, 14)
    flush()
    d.save(out_path)

if __name__ == '__main__':
    names = sorted(glob.glob(os.path.join(ROOT, 'Lab*.md')))
    for n in names:
        num = re.search(r'Lab(\d)', os.path.basename(n)).group(1)
        out = os.path.join(ROOT, 'docx', 'ПР%s_ХузинНИ_231-338.docx' % num)
        build(n, out, num); print(out)
