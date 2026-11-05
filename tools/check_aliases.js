const fs = require('fs');

const html = fs.readFileSync('tools/starta_page.html', 'utf8');

// Parse all starta symbols
const syms = [...html.matchAll(/href="\/symbol\/([^"]+)"/g)].map(m => m[1]);
const uniqueSyms = [...new Set(syms)];
console.log('Total Starta unique symbols:', uniqueSyms.length);

// Let's filter suspicious ones
console.log('ISIN-like symbols on Starta:', uniqueSyms.filter(s => s.startsWith('EGS')));
console.log('Symbols on Starta matching ANFI/TYCN/AIH/FERC:', uniqueSyms.filter(s => /ANFI|TYCN|AIH|FERC|KORA|NDRL/i.test(s)));

