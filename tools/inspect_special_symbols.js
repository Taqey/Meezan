const fs = require('fs');

const html = fs.readFileSync('tools/starta_page.html', 'utf8');

// Parse rows
const tbodyStart = html.indexOf('<tbody');
const tbodyEnd = html.indexOf('</tbody>');
const tbodyHtml = html.substring(tbodyStart, tbodyEnd + 8);
const rowMatches = tbodyHtml.split(/<tr[\s>]/).slice(1);

const rows = [];
for (const row of rowMatches) {
  const symMatch = row.match(/href="\/symbol\/([^"]+)"/);
  if (!symMatch) continue;
  const symbol = symMatch[1].trim();

  const arMatch = row.match(/dir="rtl"[^>]*>([^<]+)</);
  const nameAr = arMatch ? arMatch[1].trim() : '';

  const enMatch = row.match(/href="\/symbol\/[^"]+">([^<]+)<\/a>/);
  const nameEn = enMatch ? enMatch[1].trim() : '';

  const cellMatches = row.match(/<td[^>]*>([\s\S]*?)<\/td>/g) || [];
  const strip = s => s.replace(/<[^>]+>/g, '').replace(/[⁦⁩]/g, '').trim();

  const rawSector = cellMatches.length > 3 ? strip(cellMatches[3]) : '';
  const rawPrice = cellMatches.length > 4 ? strip(cellMatches[4]) : '';
  const rawChange = cellMatches.length > 5 ? strip(cellMatches[5]) : '';

  rows.push({ symbol, nameEn, nameAr, rawSector, rawPrice, rawChange });
}

// Let's print the ISIN-like and special ones
const special = ['EGS385S1C012', 'EGS65621C012', 'EGS65101C015', 'EGS370O1C013', 'EGS220N1C016', 'EGS65861C014', 'EGS72L31C011', 'EGS73M81C012', 'AIH', 'TYCN', 'NDRL', 'KORA'];
console.log(rows.filter(r => special.includes(r.symbol)));

