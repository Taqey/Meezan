const fs = require('fs');
const path = require('path');

const filePath = 'backend/src/Meezan.Application/Common/Models/Funds/SndukModels.cs';
const content = fs.readFileSync(filePath, 'utf8').replace(/\r\n/g, '\n');

const header = `using System.Text.Json.Serialization;

namespace Meezan.Application.Common.Models.Funds;
`;

const classRegex = /\n(?=public sealed class [A-Za-z0-9_]+)/g;
const chunks = content.substring(content.indexOf('public sealed class')).split(classRegex);

console.log('Total chunks:', chunks.length);

chunks.forEach(chunk => {
  const match = chunk.match(/public sealed class ([A-Za-z0-9_]+)/);
  if (!match) return;
  const className = match[1];
  const target = path.join('backend/src/Meezan.Application/Common/Models/Funds', className + '.cs');
  fs.writeFileSync(target, header + '\n' + chunk.trim() + '\n', 'utf8');
  console.log('Wrote', className);
});

fs.unlinkSync(filePath);
console.log('Deleted monolithic SndukModels.cs');
