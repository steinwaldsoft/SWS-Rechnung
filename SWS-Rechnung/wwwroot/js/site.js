/* SWS-Rechnung – Frontend JS */

// ── Positions-Editor ─────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    calcAllSums();
});

function addPosition() {
    const c   = document.getElementById('posContainer');
    const idx = c.querySelectorAll('.pos-row').length;
    const tmpl= document.getElementById('posTmpl').innerHTML;
    const html= tmpl.replace(/__I__/g, idx).replace(/__N__/g, idx + 1);
    c.insertAdjacentHTML('beforeend', html);
    renumberPositions();
}

function removePosition(btn) {
    btn.closest('.pos-row').remove();
    renumberPositions();
    calcAllSums();
}

function renumberPositions() {
    const rows = document.querySelectorAll('#posContainer .pos-row');
    rows.forEach((row, i) => {
        row.querySelector('.pos-nr-badge').textContent = i + 1;
        row.querySelectorAll('[name]').forEach(el => {
            el.name = el.name.replace(/\[\d+\]/, `[${i}]`);
        });
        const pf = row.querySelector('.pos-pos-field');
        if (pf) pf.value = i + 1;
    });
}

function calcAllSums() {
    let total = 0;
    document.querySelectorAll('#posContainer .pos-row').forEach(row => {
        const menge = parseFloat(row.querySelector('.f-menge')?.value)  || 0;
        const ep    = parseFloat(row.querySelector('.f-ep')?.value)     || 0;
        const rbt   = parseFloat(row.querySelector('.f-rabatt')?.value) || 0;
        const sum   = menge * ep * (1 - rbt / 100);
        total += sum;
        const el = row.querySelector('.pos-sum');
        if (el) el.textContent = fmtEur(sum);
    });
    const mwst = parseFloat(document.getElementById('mwstField')?.value) || 19;
    const mwstAmt = total * mwst / 100;
    const brutto  = total + mwstAmt;
    setText('sumNetto',  fmtEur(total));
    setText('sumMwSt',   fmtEur(mwstAmt));
    setText('sumBrutto', fmtEur(brutto));
}

function fmtEur(v) {
    return new Intl.NumberFormat('de-DE', { style:'currency', currency:'EUR' }).format(v);
}
function setText(id, txt) {
    const el = document.getElementById(id);
    if (el) el.textContent = txt;
}

// Listen to mwst changes
document.addEventListener('change', e => {
    if (e.target?.id === 'mwstField') calcAllSums();
});

// ── Confirm helpers ───────────────────────────────────────────────
function confirmDel(name) {
    return confirm(`„${name}" wirklich löschen?\nDiese Aktion kann nicht rückgängig gemacht werden.`);
}
function confirmAct(msg) {
    return confirm(msg);
}

// ── Auto-dismiss alerts after 6 s ────────────────────────────────
setTimeout(() => {
    document.querySelectorAll('.sws-alert').forEach(el => {
        el.style.transition = 'opacity .4s';
        el.style.opacity = '0';
        setTimeout(() => el.remove(), 400);
    });
}, 6000);
