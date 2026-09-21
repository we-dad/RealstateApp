#!/usr/bin/env python3
"""Generates docs/dashboard.html from ROADMAP.md, PROGRESS.md and TODAY.md.

Usage (from the repo root):  python3 docs/generate_dashboard.py
Uses only the Python standard library. Reads the three planning files and
writes one self-contained HTML file (no external libraries, works offline).
A finished task is crossed out on the page, so it is enough to tick its
checkboxes in ROADMAP.md (or mark it done in PROGRESS.md) and run this again.
"""
import html
import re
import sys
from datetime import datetime
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "docs" / "dashboard.html"

DONE, DOING, TODO = "done", "doing", "todo"
STATUS_LABEL = {DONE: "منجزة", DOING: "قيد التنفيذ", TODO: "لم تبدأ"}
TASK_WORD = r"(?:مهمة|بند)"  # old files may still say "بند"


def read(name):
    path = ROOT / name
    return path.read_text(encoding="utf-8") if path.exists() else ""


def esc(text):
    return html.escape(str(text).strip(), quote=True)


def clean(text):
    """Strip markdown emphasis/code marks for plain display."""
    return re.sub(r"[`*]", "", text).strip()


def parse_roadmap(text):
    """Returns {id: task} from the overview table and the checkbox sections."""
    items = {}
    for line in text.splitlines():
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if len(cells) >= 5 and cells[1].isdigit() and cells[0].isdigit():
            items[int(cells[1])] = {
                "id": int(cells[1]),
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
            m = re.search(TASK_WORD + r"\s*(\d+)", cells[1])
            rows.append({
                "date": cells[0],
                "task": clean(cells[1]),
                "status": clean(cells[2]),
                "note": clean(cells[3]) if len(cells) > 3 else "",
                "item": int(m.group(1)) if m else None,
            })
    return rows


def parse_today(text):
    """Returns (date, tasks, current_task_id)."""
    date_m = re.search(r"مهام اليوم\s*[—-]\s*(\d{4}-\d{2}-\d{2})", text)
    tasks, current = [], None
    for line in text.splitlines():
        if line.startswith("## "):
            title = line[3:].strip()
            if title.startswith("مهام الأيام") or title.startswith("مهام الغد") or title.startswith("ما أُنجز"):
                current = None
                continue
            done = "✅" in title
            title = clean(re.sub(r"^\d+\.\s*", "", title).replace("✅", ""))
            title = re.sub(r"^(?:منجز|منجزة):\s*", "", title)
            m = re.search(TASK_WORD + r"\s*(\d+)", title)
            current = {"title": title, "done": done, "reason": "",
                       "item": int(m.group(1)) if m else None}
            tasks.append(current)
        elif current is not None and line.startswith("- **السبب:**"):
            current["reason"] = clean(line.replace("- **السبب:**", ""))
    cur_m = re.search(r"(?:المهمة|البند) الحالي[ةه]?:\s*#(\d+)", text)
    return (date_m.group(1) if date_m else "", tasks, int(cur_m.group(1)) if cur_m else None)


def compute_status(items, progress, current_id):
    status, latest = {}, {}
    for row in progress:  # newest first: keep the first per task
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
:root{
  --bg:#f3f5f9;--card:#ffffff;--text:#172033;--muted:#6b7688;--line:#e6eaf1;
  --accent:#4f46e5;--accent2:#0ea5a4;
  --done:#16a34a;--done-bg:#e7f6ec;--doing:#d97706;--doing-bg:#fef3dc;
  --todo:#64748b;--todo-bg:#eef1f6;--client:#7c3aed;--client-bg:#f0eaff;
  --shadow:0 1px 2px rgba(16,24,40,.05),0 8px 24px rgba(16,24,40,.06);
}
@media (prefers-color-scheme:dark){:root{
  --bg:#0e1320;--card:#171e30;--text:#e9edf6;--muted:#93a0b8;--line:#263049;
  --accent:#8b8cf8;--accent2:#2dd4bf;
  --done:#4ade80;--done-bg:#12321f;--doing:#fbbf24;--doing-bg:#3a2e0e;
  --todo:#94a3b8;--todo-bg:#232c42;--client:#b79cff;--client-bg:#2a2148;
  --shadow:0 1px 2px rgba(0,0,0,.3),0 8px 24px rgba(0,0,0,.25);
}}
*{box-sizing:border-box}
html{scroll-behavior:smooth}
body{margin:0;background:var(--bg);color:var(--text);line-height:1.65;
  font-family:-apple-system,"Segoe UI","Noto Sans Arabic","Cairo",Tahoma,Arial,sans-serif}
.wrap{max-width:1040px;margin:0 auto;padding:24px 16px 48px}
.top{display:flex;align-items:center;justify-content:space-between;gap:12px;flex-wrap:wrap;margin-bottom:20px}
h1{margin:0;font-size:1.7rem;letter-spacing:-.2px}
.sub{margin:2px 0 0;color:var(--muted);font-size:.95rem}
.pill{background:var(--card);border:1px solid var(--line);border-radius:999px;padding:6px 14px;
  font-size:.85rem;color:var(--muted);box-shadow:var(--shadow)}
.card{background:var(--card);border:1px solid var(--line);border-radius:18px;padding:20px;
  box-shadow:var(--shadow);margin-bottom:18px}
h2{margin:0 0 14px;font-size:1.1rem;display:flex;align-items:center;gap:8px}
h2 .count{font-size:.8rem;font-weight:600;color:var(--muted);background:var(--todo-bg);
  border-radius:999px;padding:1px 10px}

/* hero */
.hero{display:grid;grid-template-columns:auto 1fr;gap:28px;align-items:center;
  background:linear-gradient(135deg,var(--card),var(--card)) padding-box}
.ring{position:relative;width:150px;height:150px}
.ring svg{width:150px;height:150px;transform:rotate(90deg) scaleX(-1)}
.ring .bg{stroke:var(--todo-bg)}
.ring .fg{stroke:url(#g);stroke-linecap:round;transition:stroke-dashoffset .8s}
.ringtxt{position:absolute;inset:0;display:flex;flex-direction:column;align-items:center;justify-content:center}
.ringtxt b{font-size:2.3rem;line-height:1;color:var(--text)}
.ringtxt span{font-size:.8rem;color:var(--muted)}
.hero h2{font-size:1.35rem;margin-bottom:10px}
.seg{display:flex;height:14px;border-radius:999px;overflow:hidden;background:var(--todo-bg)}
.seg i{display:block;height:100%}
.seg .d{background:var(--done)}.seg .w{background:var(--doing)}
.tiles{display:grid;grid-template-columns:repeat(4,1fr);gap:10px;margin-top:16px}
.tile{border-radius:14px;padding:10px 12px;background:var(--todo-bg)}
.tile b{display:block;font-size:1.4rem;line-height:1.2}
.tile span{font-size:.8rem;color:var(--muted)}
.tile.done{background:var(--done-bg)}.tile.done b{color:var(--done)}
.tile.doing{background:var(--doing-bg)}.tile.doing b{color:var(--doing)}
.tile.steps{background:var(--client-bg)}.tile.steps b{color:var(--client)}

/* current */
.cur{border-inline-start:6px solid var(--doing);display:flex;gap:14px;align-items:center;flex-wrap:wrap}
.cur .lbl{font-size:.8rem;color:var(--doing);font-weight:700}
.cur .ttl{font-size:1.15rem;font-weight:700}
.cur .meta{color:var(--muted);font-size:.9rem}

.grid2{display:grid;grid-template-columns:1fr 1fr;gap:18px}
ul.list{list-style:none;margin:0;padding:0}
ul.list li{display:flex;gap:10px;padding:11px 0;border-top:1px solid var(--line)}
ul.list li:first-child{border-top:0;padding-top:0}
.box{flex:none;width:22px;height:22px;border-radius:7px;border:2px solid var(--line);
  display:flex;align-items:center;justify-content:center;font-size:.8rem;color:#fff;margin-top:2px}
li.done .box{background:var(--done);border-color:var(--done)}
li.done .box::before{content:"✓"}
.done .ttl,tr.done .name,.task.done .ttl{text-decoration:line-through;text-decoration-thickness:2px;
  text-decoration-color:var(--done);color:var(--muted)}
.small{color:var(--muted);font-size:.85rem}
.time{flex:none;width:10px;height:10px;border-radius:50%;background:var(--done);margin-top:9px}

/* client tasks */
.tasks{display:grid;grid-template-columns:repeat(auto-fill,minmax(290px,1fr));gap:14px}
.task{border:1px solid var(--line);border-radius:16px;padding:14px 14px 12px;
  border-inline-start:5px solid var(--todo);background:var(--card)}
.task.doing{border-inline-start-color:var(--doing)}
.task.done{border-inline-start-color:var(--done);background:var(--done-bg)}
.task .row{display:flex;justify-content:space-between;gap:8px;align-items:flex-start}
.task .ttl{font-weight:700;font-size:1rem}
.task .meta{margin-top:6px;color:var(--muted);font-size:.83rem}
.mini{height:6px;border-radius:999px;background:var(--todo-bg);margin-top:10px;overflow:hidden}
.mini i{display:block;height:100%;background:linear-gradient(90deg,var(--accent),var(--accent2));border-radius:999px}
.task.done .mini i{background:var(--done)}

.tag{display:inline-block;padding:1px 10px;border-radius:999px;font-size:.78rem;font-weight:700;white-space:nowrap}
.tag.done{background:var(--done-bg);color:var(--done)}
.tag.doing{background:var(--doing-bg);color:var(--doing)}
.tag.todo{background:var(--todo-bg);color:var(--todo)}
.tag.client{background:var(--client-bg);color:var(--client)}

/* table */
.tools{display:flex;gap:10px;flex-wrap:wrap;align-items:center;margin-bottom:12px}
.chip{border:1px solid var(--line);background:var(--card);color:var(--text);border-radius:999px;
  padding:5px 14px;font:inherit;font-size:.85rem;cursor:pointer}
.chip.on{background:var(--accent);border-color:var(--accent);color:#fff}
.search{flex:1;min-width:160px;border:1px solid var(--line);border-radius:999px;padding:6px 14px;
  background:var(--card);color:var(--text);font:inherit;font-size:.9rem}
.tblwrap{overflow-x:auto}
table{width:100%;border-collapse:collapse;font-size:.92rem}
th,td{text-align:right;padding:10px 10px;border-bottom:1px solid var(--line);vertical-align:middle}
th{color:var(--muted);font-weight:600;font-size:.8rem}
tr.done td:first-child{border-inline-start:4px solid var(--done)}
tr.doing td:first-child{border-inline-start:4px solid var(--doing)}
tr.todo td:first-child{border-inline-start:4px solid var(--todo)}
td.num{color:var(--muted);width:44px}
td.prog{min-width:110px}
tr.hidden{display:none}
.empty{color:var(--muted);padding:6px 0}
footer{color:var(--muted);text-align:center;font-size:.85rem;margin-top:26px}
footer code{background:var(--todo-bg);padding:1px 8px;border-radius:8px}

@media (max-width:760px){
  .hero{grid-template-columns:1fr;justify-items:center;text-align:center}
  .tiles{grid-template-columns:repeat(2,1fr)}
  .grid2{grid-template-columns:1fr}
  .hide-m{display:none}
  th,td{padding:9px 6px}
}
"""

JS = """
(function(){
  var rows=[].slice.call(document.querySelectorAll('#all tbody tr'));
  var chips=[].slice.call(document.querySelectorAll('.chip'));
  var box=document.getElementById('q');
  var f='all';
  function apply(){
    var q=(box.value||'').trim().toLowerCase();
    rows.forEach(function(r){
      var okS=(f==='all')||r.getAttribute('data-status')===f;
      var okQ=!q||r.textContent.toLowerCase().indexOf(q)!==-1;
      r.classList.toggle('hidden',!(okS&&okQ));
    });
  }
  chips.forEach(function(c){c.addEventListener('click',function(){
    chips.forEach(function(x){x.classList.remove('on')});
    c.classList.add('on');f=c.getAttribute('data-f');apply();
  })});
  box.addEventListener('input',apply);
})();
"""


def bar(checked, total):
    pct = round(100 * checked / total) if total else 0
    return '<div class="mini"><i style="width:%d%%"></i></div>' % pct


def build(items, status, progress, today):
    today_date, tasks, cur_id = today
    total = len(items)
    counts = {s: sum(1 for v in status.values() if v == s) for s in (DONE, DOING, TODO)}
    pct = round(100 * counts[DONE] / total) if total else 0
    seg_d = 100 * counts[DONE] / total if total else 0
    seg_w = 100 * counts[DOING] / total if total else 0
    steps_done = sum(i["checked"] for i in items.values())
    steps_all = sum(i["total"] for i in items.values())

    circ = 339.29
    offset = circ * (1 - pct / 100)

    # current task
    if cur_id in items:
        t = items[cur_id]
        steps = ("%d من %d خطوات" % (t["checked"], t["total"])) if t["total"] else ""
        cur_html = (
            '<section class="card cur"><div><div class="lbl">المهمة الحالية</div>'
            '<div class="ttl">#%d — %s</div>'
            '<div class="meta">%s%s</div></div></section>'
            % (cur_id, esc(t["title"]), esc(t["size"]), (" · " + esc(steps)) if steps else ""))
    else:
        cur_html = ""

    # today's tasks
    if tasks:
        lis = []
        for t in tasks:
            is_done = t["done"] or (t["item"] in status and status[t["item"]] == DONE)
            reason = '<div class="small">%s</div>' % esc(t["reason"]) if t["reason"] else ""
            lis.append('<li class="%s"><span class="box"></span><div><div class="ttl">%s</div>%s</div></li>'
                       % ("done" if is_done else "", esc(t["title"]), reason))
        today_html = '<ul class="list">%s</ul>' % "".join(lis)
    else:
        today_html = '<p class="empty">لا توجد مهام لليوم.</p>'
    today_label = ("مهام يوم %s" % esc(today_date)) if today_date else "مهام اليوم"

    # latest achievements
    done_rows = sorted([r for r in progress if r["status"] == "منجز"], key=lambda r: r["date"], reverse=True)
    if done_rows[:5]:
        lis = []
        for r in done_rows[:5]:
            note = '<div class="small">%s</div>' % esc(r["note"][:170] + ("…" if len(r["note"]) > 170 else "")) if r["note"] else ""
            lis.append('<li><span class="time"></span><div><div class="ttl" style="text-decoration:none;color:inherit">'
                       '<b>%s</b></div><div class="small">%s</div>%s</div></li>'
                       % (esc(r["task"]), esc(r["date"]), note))
        ach_html = '<ul class="list">%s</ul>' % "".join(lis)
    else:
        ach_html = '<p class="empty">لا توجد إنجازات مسجلة بعد.</p>'

    # all tasks table: the tasks in the order of ROADMAP.md
    rows = []
    for t in items.values():
        st = status[t["id"]]
        prog = ("%d/%d" % (t["checked"], t["total"])) if t["total"] else "—"
        rows.append(
            '<tr class="%s" data-status="%s"><td class="num">%d</td>'
            '<td class="name">%s</td><td>%s</td><td><span class="tag %s">%s</span></td>'
            '<td class="prog hide-m"><span class="small">%s</span>%s</td><td class="hide-m small">%s</td></tr>'
            % (st, st, t["id"], esc(t["title"]), esc(t["size"]), st, STATUS_LABEL[st],
               prog, bar(t["checked"], t["total"]), esc(t["note"])))

    now = datetime.now().strftime("%Y-%m-%d %H:%M")

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

<header class="top">
  <div><h1>لوحة تقدم المشروع</h1><p class="sub">RealEstateInstallmentsManager — مهام المشروع</p></div>
  <span class="pill">آخر تحديث: {now}</span>
</header>

<section class="card hero">
  <div class="ring" role="img" aria-label="نسبة الإنجاز {pct}%">
    <svg viewBox="0 0 120 120">
      <defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0" stop-color="#4f46e5"/><stop offset="1" stop-color="#0ea5a4"/></linearGradient></defs>
      <circle class="bg" cx="60" cy="60" r="54" fill="none" stroke-width="11"/>
      <circle class="fg" cx="60" cy="60" r="54" fill="none" stroke-width="11"
              stroke-dasharray="{circ}" stroke-dashoffset="{offset:.2f}"/>
    </svg>
    <div class="ringtxt"><b>{pct}%</b><span>منجزة</span></div>
  </div>
  <div>
    <h2>{counts[DONE]} من {total} مهمة منجزة</h2>
    <div class="seg" aria-hidden="true"><i class="d" style="width:{seg_d:.1f}%"></i><i class="w" style="width:{seg_w:.1f}%"></i></div>
    <div class="tiles">
      <div class="tile done"><b>{counts[DONE]}</b><span>منجزة</span></div>
      <div class="tile doing"><b>{counts[DOING]}</b><span>قيد التنفيذ</span></div>
      <div class="tile"><b>{counts[TODO]}</b><span>لم تبدأ</span></div>
      <div class="tile steps"><b>{steps_done}/{steps_all}</b><span>خطوات منجزة</span></div>
    </div>
  </div>
</section>

{cur_html}

<div class="grid2">
  <section class="card"><h2>{today_label}</h2>{today_html}</section>
  <section class="card"><h2>آخر الإنجازات</h2>{ach_html}</section>
</div>

<section class="card" id="all">
  <h2>كل المهام <span class="count">{total}</span></h2>
  <div class="tools">
    <button class="chip on" data-f="all">الكل</button>
    <button class="chip" data-f="doing">قيد التنفيذ</button>
    <button class="chip" data-f="todo">لم تبدأ</button>
    <button class="chip" data-f="done">منجزة</button>
    <input class="search" id="q" type="search" placeholder="ابحث في المهام...">
  </div>
  <div class="tblwrap"><table>
    <thead><tr><th>#</th><th>المهمة</th><th>الحجم</th><th>الحالة</th><th class="hide-m">الخطوات</th><th class="hide-m">ملاحظة</th></tr></thead>
    <tbody>{"".join(rows)}</tbody>
  </table></div>
</section>

<footer>إذا خلصت مهمة تُشطب تلقائيًا بعد تحديث الملف: <code>python3 docs/generate_dashboard.py</code></footer>

</div>
<script>{JS}</script>
</body>
</html>
"""


def main():
    items = parse_roadmap(read("ROADMAP.md"))
    if not items:
        sys.exit("ROADMAP.md: no tasks found")
    progress = parse_progress(read("PROGRESS.md"))
    today = parse_today(read("TODAY.md"))
    status = compute_status(items, progress, today[2])
    OUT.write_text(build(items, status, progress, today), encoding="utf-8")
    done_n = sum(1 for s in status.values() if s == DONE)
    print("wrote %s: %d/%d tasks done" % (OUT.relative_to(ROOT), done_n, len(items)))


if __name__ == "__main__":
    main()
