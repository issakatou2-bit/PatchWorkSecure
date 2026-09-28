const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');

// 原音は上書きせず、PCMを変更しない再生用コピーを作る。
const destination = path.join(__dirname, 'listening');
fs.mkdirSync(destination, { recursive: true });
for (const name of fs.readdirSync(__dirname).filter(name => name.endsWith('.wav'))) {
  const original = fs.readFileSync(path.join(__dirname, name));
  if (original.toString('ascii', 0, 4) !== 'RIFF' || original.toString('ascii', 8, 12) !== 'WAVE' ||
      original.readUInt32LE(4) + 8 !== original.length) throw new Error(name + ': WAVサイズ不一致');
  let format, data;
  for (let cursor = 12; cursor + 8 <= original.length;) {
    const kind = original.toString('ascii', cursor, cursor + 4);
    const size = original.readUInt32LE(cursor + 4);
    if (cursor + 8 + size > original.length) throw new Error(name + ': WAVチャンク破損');
    if (kind === 'fmt ') format = cursor + 8;
    if (kind === 'data') data = original.subarray(cursor + 8, cursor + 8 + size);
    cursor += 8 + size + (size % 2);
  }
  if (format === undefined || !data || original.readUInt16LE(format) !== 1)
    throw new Error(name + ': PCMのfmt/dataが必要');
  const channels = original.readUInt16LE(format + 2);
  const sampleRate = original.readUInt32LE(format + 4);
  const alignment = original.readUInt16LE(format + 12);
  const bits = original.readUInt16LE(format + 14);
  if (alignment !== channels * bits / 8 || data.length % alignment !== 0)
    throw new Error(name + ': PCMのフレーム構造不一致');
  const prepared = Buffer.from(original);
  // Studio出力のByteRateだけが96000。24kHz/mono/16bitの48000へ正規化。
  prepared.writeUInt32LE(sampleRate * alignment, format + 8);
  if (!prepared.subarray(format + 12).equals(original.subarray(format + 12)))
    throw new Error(name + ': 原音が変更された');
  fs.writeFileSync(path.join(destination, name), prepared);
  process.stdout.write(JSON.stringify({ name, seconds: data.length / (sampleRate * alignment),
    sampleRate, channels, bits, pcmSha256: crypto.createHash('sha256').update(data).digest('hex') }) + '\n');
}
