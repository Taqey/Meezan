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

// Stocks in DB: let's query with a simple script
fs.writeFileSync('tools/starta_parsed.json', JSON.stringify(rows, null, 2));
console.log('Saved tools/starta_parsed.json');

