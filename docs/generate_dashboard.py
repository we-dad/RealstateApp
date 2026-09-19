#!/usr/bin/env python3
"""Generates docs/dashboard.html from ROADMAP.md, PROGRESS.md and TODAY.md.

Usage (from the repo root):  python3 docs/generate_dashboard.py
Uses only the Python standard library. Reads the three planning files and
writes one self-contained HTML file; it touches nothing else.
"""
import html
import re
import sys
from datetime import datetime
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "docs" / "dashboard.html"

DONE, DOING, TODO = "done", "doing", "todo"
STATUS_LABEL = {DONE: "منجز", DOING: "قيد التنفيذ", TODO: "لم يبدأ"}


def read(name):
    path = ROOT / name
    return path.read_text(encoding="utf-8") if path.exists() else ""


def esc(text):
    return html.escape(text.strip(), quote=True)


def clean(text):
    """Strip markdown emphasis/code marks for plain display."""
    return re.sub(r"[`*]", "", text).strip()


def parse_roadmap(text):
    """Returns {id: item} using the overview table and the checkbox sections."""
    items = {}
    for line in text.splitlines():
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if len(cells) >= 5 and cells[1].isdigit() and cells[0].isdigit():
            items[int(cells[1])] = {
                "id": int(cells[1]),
                "priority": int(cells[0]),
                "title": clean(cells[2]),
                "size": clean(cells[3]),
                "order": clean(cells[4]),
                "note": clean(cells[5]) if len(cells) > 5 else "",
                "checked": 0,
                "total": 0,
            }
    current = None
    for line in text.splitlines():
        m = re.match(r"^##\s+(\d+)\.\s", line)
        if m:
            current = int(m.group(1))
            continue
        if line.startswith("## "):
            current = None
            continue
        m = re.match(r"^- \[( |x|X)\]", line)
        if m and current in items:
            items[current]["total"] += 1
            if m.group(1) in "xX":
                items[current]["checked"] += 1
    return items


def parse_progress(text):
    rows = []
    for line in text.splitlines():
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if len(cells) >= 3 and re.match(r"^\d{4}-\d{2}-\d{2}", cells[0]):
            m = re.search(r"بند\s*(\d+)", cells[1])
            rows.append({
                "date": cells[0],
                "task": clean(cells[1]),
                "status": clean(cells[2]),
                "note": clean(cells[3]) if len(cells) > 3 else "",
                "item": int(m.group(1)) if m else None,
            })
    return rows


def parse_today(text):
    """Returns (date, tasks, current_item_id, current_item_text)."""
    date_m = re.search(r"مهام اليوم\s*[—-]\s*(\d{4}-\d{2}-\d{2})", text)
    tasks, current = [], None
    for line in text.splitlines():
        if line.startswith("## "):
            title = line[3:].strip()
            if title.startswith("مهام الغد") or title.startswith("ما أُنجز"):
                current = None
                continue
            done = "✅" in title or "منجز" in title.split(":")[0]
            title = clean(re.sub(r"^\d+\.\s*", "", title).replace("✅", ""))
            title = re.sub(r"^منجز:\s*", "", title)
            m = re.search(r"بند\s*(\d+)", title)
            current = {"title": title, "done": done, "reason": "",
                       "item": int(m.group(1)) if m else None}
            tasks.append(current)
        elif current is not None and line.startswith("- **السبب:**"):
            current["reason"] = clean(line.replace("- **السبب:**", ""))
    cur_m = re.search(r"البند الحالي:\s*#(\d+)\s*[—-]?\s*(.*)", text)
    cur_id = int(cur_m.group(1)) if cur_m else None
    cur_text = clean(cur_m.group(2)) if cur_m else ""
    return (date_m.group(1) if date_m else "", tasks, cur_id, cur_text)


def compute_status(items, progress, current_id):
    status = {}
    latest = {}
    for row in progress:  # rows are newest first; keep the first per item
        if row["item"] is not None and row["item"] not in latest:
            latest[row["item"]] = row["status"]
    for i, it in items.items():
        all_checked = it["total"] > 0 and it["checked"] == it["total"]
        if all_checked or latest.get(i) == "منجز":
            status[i] = DONE
        elif it["checked"] > 0 or latest.get(i) == "قيد التنفيذ" or i == current_id:
            status[i] = DOING
        else:
            status[i] = TODO
    return status


CSS = """
:root{--bg:#f5f6f8;--card:#fff;--text:#1c2430;--muted:#667085;--line:#e4e7ec;
--done:#1f9d55;--done-bg:#e3f5ea;--doing:#b7791f;--doing-bg:#fdf1cf;
--todo:#667085;--todo-bg:#eceef2;--accent:#2563eb}
@media (prefers-color-scheme:dark){:root{--bg:#12161c;--card:#1b212a;--text:#e8ecf2;
--muted:#9aa4b2;--line:#2c3542;--done:#4ade80;--done-bg:#153a25;--doing:#fbbf24;
--doing-bg:#3f3210;--todo:#9aa4b2;--todo-bg:#262d38;--accent:#6ea0ff}}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--text);
font-family:-apple-system,"Segoe UI",Tahoma,"Noto Sans Arabic",Arial,sans-serif;
line-height:1.6}
.wrap{max-width:980px;margin:0 auto;padding:20px 16px 40px}
h1{font-size:1.5rem;margin:0 0 4px}
.sub{color:var(--muted);margin:0 0 20px;font-size:.95rem}
h2{font-size:1.05rem;margin:0 0 12px}
.card{background:var(--card);border:1px solid var(--line);border-radius:12px;
padding:16px;margin-bottom:16px}
.hero{display:flex;align-items:center;gap:16px;flex-wrap:wrap;margin-bottom:12px}
.pct{font-size:2.6rem;font-weight:700;color:var(--done);line-height:1}
.hero .lbl{color:var(--muted)}
.bar{height:14px;background:var(--todo-bg);border-radius:999px;overflow:hidden}
.bar>span{display:block;height:100%;background:var(--done);border-radius:999px}
.cur{margin-top:14px;padding:10px 12px;background:var(--doing-bg);
border-radius:8px;color:var(--text)}
.cur b{color:var(--doing)}
.grid{display:grid;gap:16px;grid-template-columns:1fr 1fr}
@media (max-width:720px){.grid{grid-template-columns:1fr}}
ul.list{list-style:none;margin:0;padding:0}
ul.list li{padding:10px 0;border-top:1px solid var(--line)}
ul.list li:first-child{border-top:0;padding-top:0}
.small{color:var(--muted);font-size:.85rem}
.tag{display:inline-block;padding:1px 10px;border-radius:999px;font-size:.8rem;
font-weight:600;white-space:nowrap}
.tag.done{background:var(--done-bg);color:var(--done)}
.tag.doing{background:var(--doing-bg);color:var(--doing)}
.tag.todo{background:var(--todo-bg);color:var(--todo)}
.tblwrap{overflow-x:auto}
table{width:100%;border-collapse:collapse;font-size:.92rem}
th,td{text-align:right;padding:9px 10px;border-bottom:1px solid var(--line);
vertical-align:top}
th{color:var(--muted);font-weight:600;font-size:.82rem}
tr.done td:first-child{border-right:4px solid var(--done)}
tr.doing td:first-child{border-right:4px solid var(--doing)}
tr.todo td:first-child{border-right:4px solid var(--todo)}
@media (max-width:640px){.hide-m{display:none}th,td{padding:8px 6px}}
.empty{color:var(--muted)}
footer{color:var(--muted);text-align:center;font-size:.85rem;margin-top:24px}
"""


def build(items, status, progress, today):
    today_date, tasks, cur_id, cur_text = today
    total = len(items)
    done_n = sum(1 for s in status.values() if s == DONE)
    pct = round(100 * done_n / total) if total else 0

    if cur_id in items:
        cur_title = items[cur_id]["title"]
        cur_html = ('<div class="cur"><b>البند الحالي:</b> #%d — %s</div>'
                    % (cur_id, esc(cur_title)))
    elif cur_text:
        cur_html = '<div class="cur"><b>البند الحالي:</b> %s</div>' % esc(cur_text)
    else:
        cur_html = '<div class="cur"><b>البند الحالي:</b> غير محدد</div>'

    if tasks:
        lis = []
        for t in tasks:
            st = DONE if t["done"] else TODO
            lab = "منجز" if t["done"] else "قيد الانتظار"
            reason = ('<div class="small">%s</div>' % esc(t["reason"])) if t["reason"] else ""
            lis.append('<li><span class="tag %s">%s</span> %s%s</li>'
                       % (st, lab, esc(t["title"]), reason))
        tasks_html = '<ul class="list">%s</ul>' % "".join(lis)
    else:
        tasks_html = '<p class="empty">لا توجد مهام اليوم (TODAY.md غير موجود أو فارغ).</p>'

    done_rows = [r for r in progress if r["status"] == "منجز"]
    done_rows.sort(key=lambda r: r["date"], reverse=True)  # stable: keeps file order
    if done_rows[:5]:
        lis = []
        for r in done_rows[:5]:
            note = ('<div class="small">%s</div>' % esc(r["note"])) if r["note"] else ""
            lis.append('<li><b>%s</b> <span class="small">· %s</span>%s</li>'
                       % (esc(r["task"]), esc(r["date"]), note))
        ach_html = '<ul class="list">%s</ul>' % "".join(lis)
    else:
        ach_html = '<p class="empty">لا توجد إنجازات مسجّلة بعد.</p>'

    rows = []
    for it in sorted(items.values(), key=lambda x: x["id"]):
        st = status[it["id"]]
        prog = ("%d/%d" % (it["checked"], it["total"])) if it["total"] else "—"
        rows.append(
            '<tr class="%s"><td>%d</td><td>%s</td><td>%s</td>'
            '<td><span class="tag %s">%s</span></td><td>%s</td>'
            '<td class="hide-m">%s</td></tr>'
            % (st, it["id"], esc(it["title"]), esc(it["size"]), st,
               STATUS_LABEL[st], prog, esc(it["note"])))

    now = datetime.now().strftime("%Y-%m-%d %H:%M")
    day = ("مهام يوم %s" % esc(today_date)) if today_date else "مهام اليوم"
    return f"""<!DOCTYPE html>
<html lang="ar" dir="rtl">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>لوحة تقدم المشروع</title>
<style>{CSS}</style>
</head>
<body>
<div class="wrap">
<h1>لوحة تقدم المشروع</h1>
<p class="sub">RealEstateInstallmentsManager — خارطة الطريق</p>

<section class="card">
<div class="hero">
<div class="pct">{pct}%</div>
<div class="lbl">منجز {done_n} من {total} بندًا</div>
</div>
<div class="bar" role="progressbar" aria-valuenow="{pct}" aria-valuemin="0" aria-valuemax="100"><span style="width:{pct}%"></span></div>
{cur_html}
</section>

<div class="grid">
<section class="card"><h2>{day}</h2>{tasks_html}</section>
<section class="card"><h2>آخر 5 إنجازات</h2>{ach_html}</section>
</div>

<section class="card">
<h2>كل البنود</h2>
<div class="tblwrap"><table>
<thead><tr><th>#</th><th>البند</th><th>الحجم</th><th>الحالة</th><th>المهام</th><th class="hide-m">ملاحظة</th></tr></thead>
<tbody>{"".join(rows)}</tbody>
</table></div>
</section>

<footer>آخر تحديث: {now}</footer>
</div>
</body>
</html>
"""


def main():
    items = parse_roadmap(read("ROADMAP.md"))
    if not items:
        sys.exit("ROADMAP.md: no items found")
    progress = parse_progress(read("PROGRESS.md"))
    today = parse_today(read("TODAY.md"))
    status = compute_status(items, progress, today[2])
    OUT.write_text(build(items, status, progress, today), encoding="utf-8")
    done_n = sum(1 for s in status.values() if s == DONE)
    print("wrote %s: %d/%d done" % (OUT.relative_to(ROOT), done_n, len(items)))


if __name__ == "__main__":
    main()
