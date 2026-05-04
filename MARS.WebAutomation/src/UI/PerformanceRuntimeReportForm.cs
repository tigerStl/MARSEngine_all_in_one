using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using MARS.WebAutomation.Services;
using Newtonsoft.Json;

namespace MARS.WebAutomation.UI
{
    internal sealed partial class PerformanceRuntimeReportForm : Form
    {
        private WebView2 _webView;
        private readonly PerformanceReportSnapshot _snapshot;
        private readonly string _environmentText;
        private readonly int _simUsers;
        private readonly int _durationSec;
        private readonly bool _isChinese;
        private Panel _topBar;
        private Button _btnExportJson;
        private Button _btnExportHtml;
        private string _reportJson = "{}";
        private string _reportHtml = "<html><body></body></html>";

        public PerformanceRuntimeReportForm(PerformanceReportSnapshot snapshot, string environmentText, int simUsers, int durationSec, string uiLanguage)
        {
            _snapshot = snapshot ?? new PerformanceReportSnapshot();
            _environmentText = environmentText ?? string.Empty;
            _simUsers = Math.Max(1, simUsers);
            _durationSec = Math.Max(1, durationSec);
            _isChinese = string.Equals((uiLanguage ?? "en").Trim(), "zh", StringComparison.OrdinalIgnoreCase);
            InitializeComponent();
            _btnExportJson.Click += (_, __) => ExportReportJson();
            _btnExportHtml.Click += (_, __) => ExportReportHtml();
            Load += async (_, __) =>
            {
                await _webView.EnsureCoreWebView2Async();
                _reportHtml = BuildHtml();
                _webView.NavigateToString(_reportHtml);
            };
        }

        private string BuildHtml()
        {
            var samples = _snapshot.Samples ?? new System.Collections.Generic.List<PerformanceRequestSample>();
            var sortedDurations = samples.Select(s => s.DurationMs).OrderBy(x => x).ToList();
            double P(double p)
            {
                if (sortedDurations.Count == 0) return 0;
                var idx = (int)Math.Round((sortedDurations.Count - 1) * p, MidpointRounding.AwayFromZero);
                idx = Math.Max(0, Math.Min(sortedDurations.Count - 1, idx));
                return sortedDurations[idx];
            }

            var payload = new
            {
                env = _environmentText,
                simUsers = _simUsers,
                durationSec = _durationSec,
                total = samples.Count,
                ok = samples.Count(s => s.Success),
                fail = samples.Count(s => !s.Success),
                avg = samples.Count == 0 ? 0 : samples.Average(s => s.DurationMs),
                p25 = P(0.25),
                p50 = P(0.50),
                p75 = P(0.75),
                p90 = P(0.90),
                p95 = P(0.95),
                min = sortedDurations.Count == 0 ? 0 : sortedDurations.First(),
                max = sortedDurations.Count == 0 ? 0 : sortedDurations.Last(),
                samples = samples.Select(s => new
                {
                    startedUtc = s.StartedUtc,
                    durationMs = s.DurationMs,
                    success = s.Success,
                    statusCode = s.StatusCode,
                    method = s.Method,
                    url = s.Url,
                    payload = s.Payload,
                    responseBody = s.ResponseBody,
                    transaction = s.Transaction,
                    stepName = s.StepName
                }).ToList()
            };
            var json = JsonConvert.SerializeObject(payload);
            _reportJson = json;
            var titleEnv = _isChinese ? "测试环境" : "Test Environment";
            var titleProcess = _isChinese ? "测试过程统计" : "Process Statistics";
            var titleResult = _isChinese ? "测试结果" : "Test Result";
            var titleProcessBar = _isChinese ? "测试过程 Process Bar" : "Process Bar";
            var titleLatencyDist = _isChinese ? "耗时分布图（类正态）" : "Latency Distribution (Normal-like)";
            var titleLatencyInsight = _isChinese ? "分布解析" : "Distribution Insight";
            var titleScatter = _isChinese ? "请求时序信息点图（毫秒级）" : "Request Timeline Scatter (ms)";
            var titleList = _isChinese ? "点击点后的请求列表" : "Requests In Selected Point";
            var titleDetail = _isChinese ? "请求详情" : "Request Detail";
            var detailHint = _isChinese ? "点击上方列表中的某条请求查看详情" : "Click one request row above to view details";
            var totalText = _isChinese ? "总耗时" : "Duration";
            var reqText = _isChinese ? "总请求" : "Total Requests";
            var concText = _isChinese ? "并发" : "Concurrency";
            return @"<!doctype html><html><head><meta charset='utf-8'><style>
body{font-family:Segoe UI,Arial;margin:0;background:#f8fafc;color:#0f172a}
.wrap{padding:16px}.grid{display:grid;grid-template-columns:repeat(3,minmax(280px,1fr));gap:12px}
.card{background:#fff;border:1px solid #e2e8f0;border-radius:10px;padding:12px}
h3{margin:0 0 8px 0;font-size:15px}.kv{font-size:13px;line-height:1.6}.mono{font-family:Consolas,monospace}
canvas{width:100%;height:220px;border:1px solid #e2e8f0;border-radius:8px;background:#fff}
#points{height:300px} #list{max-height:180px;overflow:auto;border:1px solid #e2e8f0;border-radius:8px;background:#fff}
table{width:100%;border-collapse:collapse;font-size:12px}td,th{border-bottom:1px solid #e2e8f0;padding:4px 6px;text-align:left}
#detail{min-height:160px;white-space:pre-wrap;font-size:12px;background:#0b1020;color:#e2e8f0;border-radius:8px;padding:10px}
</style></head><body><div class='wrap'>
<div class='grid'>
<div class='card'><h3>" + titleEnv + @"</h3><div class='kv mono' id='env'></div></div>
<div class='card'><h3>" + titleProcess + @"</h3><canvas id='bar'></canvas></div>
<div class='card'><h3>" + titleResult + @"</h3><canvas id='ratio'></canvas><div class='kv' id='stats'></div></div>
</div>
<div class='card' style='margin-top:12px'><h3>" + titleProcessBar + @"</h3><canvas id='processBar'></canvas><div class='kv' id='processHover'></div></div>
<div class='card' style='margin-top:12px'><h3>" + titleLatencyDist + @"</h3><canvas id='latencyDist'></canvas><div class='kv' id='latencyPercentiles'></div></div>
<div class='card' style='margin-top:12px'><h3>" + titleLatencyInsight + @"</h3><div class='kv' id='latencyInsight'></div></div>
<div class='card' style='margin-top:12px'><h3>" + titleScatter + @"</h3><canvas id='points'></canvas><div class='kv' id='hover'></div></div>
<div class='card' style='margin-top:12px'><h3>" + titleList + @"</h3><div id='list'></div></div>
<div class='card' style='margin-top:12px'><h3>" + titleDetail + @"</h3><div id='detail'>" + detailHint + @"</div></div>
</div><script>
const data=" + json + @";
document.getElementById('env').textContent=data.env;
document.getElementById('stats').innerHTML=
`" + totalText + @": ${data.durationSec}s<br>" + reqText + @": ${data.total} / " + concText + @": ${data.simUsers}<br>`+
`Avg ${data.avg.toFixed(2)}ms | P50 ${data.p50.toFixed(2)}ms | P75 ${data.p75.toFixed(2)}ms | P95 ${data.p95.toFixed(2)}ms`;
const bar=document.getElementById('bar').getContext('2d');
const ratio=document.getElementById('ratio').getContext('2d');
const processBar=document.getElementById('processBar').getContext('2d');
const latencyDist=document.getElementById('latencyDist').getContext('2d');
function drawSimpleBar(ctx,labels,vals,colors){const w=ctx.canvas.width=ctx.canvas.clientWidth,h=ctx.canvas.height=ctx.canvas.clientHeight;ctx.clearRect(0,0,w,h);
const max=Math.max(1,...vals);const bw=10,gap=6;let x=30;for(let i=0;i<vals.length;i++){const bh=(h-40)*(vals[i]/max);ctx.fillStyle=colors[i];ctx.fillRect(x,h-20-bh,bw,bh);ctx.save();ctx.translate(x+2,h-4);ctx.rotate(-Math.PI/2);ctx.fillStyle='#334155';ctx.font='11px Segoe UI';ctx.fillText(labels[i],0,0);ctx.restore();x+=bw+gap;}}
drawSimpleBar(bar,['OK','Fail','Total'],[data.ok,data.fail,data.total],['#22c55e','#ef4444','#64748b']);
function shade(hex,percent){
  const c=hex.replace('#',''); const n=parseInt(c,16);
  const r=(n>>16)&255,g=(n>>8)&255,b=n&255;
  const f=(v)=>Math.max(0,Math.min(255,Math.round(v*(100+percent)/100)));
  return `rgb(${f(r)},${f(g)},${f(b)})`;
}
function drawPie3D(ctx,ok,fail){
  const w=ctx.canvas.width=ctx.canvas.clientWidth,h=ctx.canvas.height=ctx.canvas.clientHeight;
  ctx.clearRect(0,0,w,h);
  const total=Math.max(1,ok+fail);
  const cx=Math.round(w*0.36), cy=Math.round(h*0.52), r=Math.max(36,Math.min(82,Math.round(Math.min(w,h)*0.26))), depth=12;
  const start=-Math.PI/2;
  const okA=(ok/total)*Math.PI*2;
  const segs=[{k:'OK',v:ok,a0:start,a1:start+okA,c:'#22c55e'},{k:'Fail',v:fail,a0:start+okA,a1:start+Math.PI*2,c:'#ef4444'}];
  // depth (shadow/extrusion)
  for(let d=depth; d>=1; d--){
    segs.forEach(s=>{
      ctx.beginPath(); ctx.moveTo(cx,cy+d);
      ctx.fillStyle=shade(s.c,-35);
      ctx.arc(cx,cy+d,r,s.a0,s.a1,false); ctx.closePath(); ctx.fill();
    });
  }
  // top
  segs.forEach(s=>{
    ctx.beginPath(); ctx.moveTo(cx,cy);
    ctx.fillStyle=s.c;
    ctx.arc(cx,cy,r,s.a0,s.a1,false); ctx.closePath(); ctx.fill();
    ctx.strokeStyle='rgba(255,255,255,0.75)'; ctx.lineWidth=1; ctx.stroke();
  });
  // highlight
  const grad=ctx.createRadialGradient(cx-r*0.3,cy-r*0.4,4,cx,cy,r);
  grad.addColorStop(0,'rgba(255,255,255,0.45)');
  grad.addColorStop(1,'rgba(255,255,255,0)');
  ctx.beginPath(); ctx.arc(cx,cy,r,0,Math.PI*2); ctx.fillStyle=grad; ctx.fill();
  // legend
  const lx=Math.round(w*0.66), ly=Math.round(h*0.30);
  const rows=[{t:'OK',v:ok,c:'#22c55e'},{t:'Fail',v:fail,c:'#ef4444'}];
  rows.forEach((it,i)=>{
    const y=ly+i*26;
    ctx.fillStyle=it.c; ctx.fillRect(lx,y,12,12);
    const pct=((it.v/total)*100).toFixed(2);
    ctx.fillStyle='#334155'; ctx.font='12px Segoe UI';
    ctx.fillText(`${it.t}: ${it.v} (${pct}%)`, lx+18, y+11);
  });
}
drawPie3D(ratio,data.ok,data.fail);
const samples=(data.samples||[]).map((s,i)=>({...s,_i:i}));
samples.sort((a,b)=>new Date(a.startedUtc)-new Date(b.startedUtc));
if(samples.length){const minT=+new Date(samples[0].startedUtc);samples.forEach(s=>s.offset=Math.round((+new Date(s.startedUtc))-minT));}
function drawProcessBar(){
 const w=processBar.canvas.width=processBar.canvas.clientWidth,h=processBar.canvas.height=processBar.canvas.clientHeight;
 processBar.clearRect(0,0,w,h);
 if(!samples.length)return;
 const maxX=Math.max(1,...samples.map(s=>s.offset));
 const binMs=3000; // strict fixed 3-second buckets
 const binN=Math.max(1,Math.ceil(maxX/binMs));
 const bins=Array.from({length:binN},()=>({ok:0,fail:0,total:0}));
 samples.forEach(s=>{const idx=Math.min(binN-1,Math.floor(s.offset/binMs));bins[idx].total++; if(s.success)bins[idx].ok++; else bins[idx].fail++;});
 const maxY=Math.max(1,...bins.map(b=>b.total)); const bw=Math.max(10,Math.floor((w-40)/Math.max(1,Math.min(binN,40)))-2); window.__processBins={bins,maxX,w,h,bw,binN,binMs};
 for(let i=0;i<binN;i++){const x=28+i*(bw+2);const b=bins[i];const hTot=(h-36)*(b.total/maxY);const hOk=(h-36)*(b.ok/maxY);const hFail=(h-36)*(b.fail/maxY);
 if(x > w-12) break;
 processBar.fillStyle='#94a3b8';processBar.fillRect(x,h-20-hTot,bw,hTot);
 processBar.fillStyle='#22c55e';processBar.fillRect(x,h-20-hOk,bw,hOk);
 processBar.fillStyle='#ef4444';processBar.fillRect(x,h-20-hFail,bw,hFail);}
}
function drawLatencyDist(){
 const w=latencyDist.canvas.width=latencyDist.canvas.clientWidth,h=latencyDist.canvas.height=latencyDist.canvas.clientHeight;
 latencyDist.clearRect(0,0,w,h);
 const arr=(samples||[]).map(s=>Number(s.durationMs)).filter(v=>isFinite(v)&&v>=0);
 if(!arr.length){
   document.getElementById('latencyPercentiles').textContent='No latency samples.';
   document.getElementById('latencyInsight').textContent='No analysis available.';
   return;
 }
 const min=Math.min(...arr), max=Math.max(...arr);
 const bins=14, span=Math.max(1,max-min), step=span/bins;
 const hist=Array.from({length:bins},()=>0);
 arr.forEach(v=>{const i=Math.min(bins-1,Math.floor((v-min)/step));hist[i]++;});
 const smooth=hist.map((v,i)=>{const a=hist[Math.max(0,i-1)],b=v,c=hist[Math.min(bins-1,i+1)];return (a+b+c)/3;});
 const maxY=Math.max(1,...hist,...smooth);
 const x0=42,y0=h-28, cw=w-56, ch=h-44;
 latencyDist.strokeStyle='#94a3b8'; latencyDist.beginPath(); latencyDist.moveTo(x0,12); latencyDist.lineTo(x0,y0); latencyDist.lineTo(x0+cw,y0); latencyDist.stroke();
 const bw=Math.max(6,Math.floor(cw/bins)-2);
 for(let i=0;i<bins;i++){
   const x=x0+Math.floor(i*(cw/bins))+1;
   const bh=Math.round(ch*(hist[i]/maxY));
   latencyDist.fillStyle='rgba(37,99,235,0.35)';
   latencyDist.fillRect(x,y0-bh,bw,bh);
 }
 latencyDist.strokeStyle='#2563eb'; latencyDist.lineWidth=2; latencyDist.beginPath();
 for(let i=0;i<bins;i++){
   const x=x0+Math.floor((i+0.5)*(cw/bins));
   const y=y0-Math.round(ch*(smooth[i]/maxY));
   if(i===0) latencyDist.moveTo(x,y); else latencyDist.lineTo(x,y);
 }
 latencyDist.stroke();
 const marks=[{k:'25%',v:data.p25,c:'#0ea5e9'},{k:'50%',v:data.p50,c:'#16a34a'},{k:'75%',v:data.p75,c:'#ca8a04'},{k:'90%',v:data.p90,c:'#f97316'},{k:'95%',v:data.p95,c:'#dc2626'}];
 marks.forEach((m,idx)=>{
   const px=x0+Math.max(0,Math.min(cw,Math.round(((m.v-min)/span)*cw)));
   latencyDist.strokeStyle=m.c; latencyDist.lineWidth=1.4;
   latencyDist.beginPath(); latencyDist.moveTo(px,12); latencyDist.lineTo(px,y0); latencyDist.stroke();
   latencyDist.fillStyle=m.c; latencyDist.font='11px Segoe UI';
   latencyDist.fillText(`${m.k}:${m.v.toFixed(1)}ms`, Math.min(px+3,w-110), 16+idx*12);
 });
 latencyDist.fillStyle='#334155'; latencyDist.font='11px Segoe UI';
 latencyDist.fillText(`${min.toFixed(1)}ms`, x0, h-8);
 latencyDist.fillText(`${max.toFixed(1)}ms`, x0+cw-45, h-8);
 document.getElementById('latencyPercentiles').innerHTML =
   `P25=${data.p25.toFixed(2)}ms | P50=${data.p50.toFixed(2)}ms | P75=${data.p75.toFixed(2)}ms | P90=${data.p90.toFixed(2)}ms | P95=${data.p95.toFixed(2)}ms`;
 const spread=(data.p75-data.p25);
 const tail=(data.p95/Math.max(1,data.p50));
 const skew=((data.p90-data.p50)-Math.max(0,(data.p50-data.p25)));
 const i18n = " + (_isChinese ? "'zh'" : "'en'") + @";
 let insight='';
 if(i18n==='zh'){
   const stability = spread<=40 ? '延迟集中，稳定性较好' : spread<=120 ? '延迟有一定波动' : '延迟离散较大，稳定性一般';
   const tailTxt = tail<=1.8 ? '长尾可控' : tail<=2.5 ? '存在明显长尾' : '长尾较重，需重点优化慢请求';
   const skewTxt = skew<=10 ? '分布较对称，接近正态形态' : '右偏明显，高延迟尾部更厚';
   insight = `结论：${stability}；${tailTxt}；${skewTxt}。建议优先排查 P90~P95 区间请求（接口慢点、重试、网络抖动或大包体）。`;
 } else {
   const stability = spread<=40 ? 'Latency is concentrated and stable' : spread<=120 ? 'Latency has moderate variance' : 'Latency is widely spread with weaker stability';
   const tailTxt = tail<=1.8 ? 'Tail is under control' : tail<=2.5 ? 'Noticeable long-tail exists' : 'Heavy long-tail; optimize slow requests first';
   const skewTxt = skew<=10 ? 'Shape is fairly symmetric and normal-like' : 'Right-skewed shape with thicker high-latency tail';
   insight = `Conclusion: ${stability}; ${tailTxt}; ${skewTxt}. Focus on requests around P90-P95 first (slow endpoints, retries, network jitter, or large payloads).`;
 }
 document.getElementById('latencyInsight').textContent = insight;
}
const pts=document.getElementById('points');const pctx=pts.getContext('2d');let groups=[];let hit=[];
function drawPoints(){
 const w=pts.width=pts.clientWidth,h=pts.height=pts.clientHeight; pctx.clearRect(0,0,w,h); pctx.strokeStyle='#94a3b8'; pctx.beginPath(); pctx.moveTo(40,h-30); pctx.lineTo(w-10,h-30); pctx.moveTo(40,10); pctx.lineTo(40,h-30); pctx.stroke();
 if(!samples.length) return; const maxX=Math.max(1,...samples.map(s=>s.offset)); const maxY=Math.max(1,...samples.map(s=>s.durationMs)); const map=new Map();
 samples.forEach(s=>{const key=s.offset; if(!map.has(key)) map.set(key,[]); map.get(key).push(s);});
 groups=[...map.entries()].map(([k,v])=>({offset:k,items:v,avg:v.reduce((a,b)=>a+b.durationMs,0)/v.length}));
 hit=[]; groups.forEach(g=>{const x=40+(w-60)*(g.offset/maxX); const y=(h-30)-(h-50)*(g.avg/maxY); const r=Math.max(3,Math.min(10,2+Math.log2(g.items.length+1))); pctx.fillStyle='rgba(37,99,235,0.55)'; pctx.beginPath(); pctx.arc(x,y,r,0,Math.PI*2); pctx.fill(); hit.push({x,y,r,g});});
}
drawPoints();drawProcessBar();drawLatencyDist(); window.addEventListener('resize',()=>{drawSimpleBar(bar,['OK','Fail','Total'],[data.ok,data.fail,data.total],['#22c55e','#ef4444','#64748b']);drawPie3D(ratio,data.ok,data.fail);drawPoints();drawProcessBar();drawLatencyDist();});
processBar.canvas.addEventListener('mousemove',e=>{const d=window.__processBins;if(!d)return;const r=processBar.canvas.getBoundingClientRect();const x=e.clientX-r.left;const idx=Math.max(0,Math.min(d.binN-1,Math.floor((x-28)/(d.bw+2))));const b=d.bins[idx]||{ok:0,fail:0,total:0};const fromSec=((idx*d.binMs)/1000).toFixed(0);const toSec=(((idx+1)*d.binMs)/1000).toFixed(0);document.getElementById('processHover').textContent=`bucket=${idx+1} (${fromSec}s-${toSec}s), total=${b.total}, ok=${b.ok}, fail=${b.fail}`;});
pts.addEventListener('mousemove',e=>{const r=pts.getBoundingClientRect();const x=e.clientX-r.left,y=e.clientY-r.top;const h=hit.find(p=>((x-p.x)**2+(y-p.y)**2)<=p.r*p.r);document.getElementById('hover').textContent=h?`offset=${h.g.offset}ms, ${h.g.items.length} requests`:' ';});
pts.addEventListener('click',e=>{const r=pts.getBoundingClientRect();const x=e.clientX-r.left,y=e.clientY-r.top;const h=hit.find(p=>((x-p.x)**2+(y-p.y)**2)<=p.r*p.r);if(!h)return;const arr=[...h.g.items].sort((a,b)=>b.durationMs-a.durationMs);
document.getElementById('list').innerHTML='<table><tr><th>#</th><th>Method</th><th>Status</th><th>Duration(ms)</th><th>URL</th></tr>'+arr.map((s,i)=>`<tr data-idx='${s._i}'><td>${i+1}</td><td>${s.method||''}</td><td>${s.statusCode}</td><td>${s.durationMs.toFixed(2)}</td><td>${(s.url||'').slice(0,90)}</td></tr>`).join('')+'</table>';
document.querySelectorAll('#list tr[data-idx]').forEach(tr=>tr.onclick=()=>{const s=samples.find(t=>t._i==tr.getAttribute('data-idx'));if(!s)return;const rank=(samples.filter(t=>t.durationMs<=s.durationMs).length/samples.length*100).toFixed(2);
document.getElementById('detail').textContent=`start: ${s.startedUtc}\ntransaction: ${s.transaction}\nstep: ${s.stepName}\nmethod: ${s.method}\nurl: ${s.url}\nstatus: ${s.statusCode}\nduration: ${s.durationMs.toFixed(2)} ms\nposition: top ${rank}%\npayload:\n${s.payload||''}\n\nresponse body length: ${(s.responseBody||'').length}\nresponse body:\n${s.responseBody||''}`;});});
</script></body></html>";
        }

        private void ExportReportJson()
        {
            using (var dlg = new SaveFileDialog
            {
                Filter = "JSON file|*.json|All files|*.*",
                FileName = "performance-runtime-report.json"
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                File.WriteAllText(dlg.FileName, _reportJson ?? "{}", Encoding.UTF8);
            }
        }

        private void ExportReportHtml()
        {
            using (var dlg = new SaveFileDialog
            {
                Filter = "HTML file|*.html|All files|*.*",
                FileName = "performance-runtime-report.html"
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                File.WriteAllText(dlg.FileName, _reportHtml ?? "<html><body></body></html>", Encoding.UTF8);
            }
        }
    }
}
