const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const script = fs.readFileSync('web/downloads.js', 'utf8');
const prefix = 'https://github.com/jakeharvey162-source/skyline-rush-ffp/releases/download/test/';
function asset(name, size = 1048576) { return { name, size, browser_download_url: prefix + name }; }
function release(parts = ['001', '002']) {
  return [{ name: 'Test build', draft: false, assets: [...parts.map(p => asset('skyline-rush-windows.7z.' + p)), asset('SHA256SUMS.txt')] }];
}
async function render(data, ok = true) {
  const nodes = {};
  const element = () => ({ children: [], append(child) { this.children.push(child); }, replaceChildren() { this.children = []; } });
  const document = { getElementById(id) { return nodes[id] ??= element(); }, createElement: element };
  await vm.runInNewContext(script, { document, URL, AbortSignal, fetch: async () => ({ ok, json: async () => data }) });
  return nodes;
}
function unavailable(nodes) {
  assert.match(nodes['release-files'].children[0].textContent, /not available/);
  assert.equal(nodes['release-files'].children.some(c => c.href), false);
}
test('complete release displays ordered parts and checksums', async () => {
  const nodes = await render(release(['002', '001']));
  assert.equal(nodes['release-files'].children.length, 3);
  assert.match(nodes['release-files'].children[0].href, /\.001$/);
  assert.match(nodes['release-files'].children[1].textContent, /part 2 of 2/);
});
test('missing middle part is not offered', async () => unavailable(await render(release(['001', '003']))));
test('missing checksums are not offered', async () => {
  const data = release(); data[0].assets.pop(); unavailable(await render(data));
});
test('empty, draft and service-error states have no fabricated links', async () => {
  unavailable(await render([]));
  const data = release(); data[0].draft = true; unavailable(await render(data));
  unavailable(await render([], false));
});
test('unexpected download origin is rejected', async () => {
  const data = release(); data[0].assets[0].browser_download_url = 'https://example.com/game.exe';
  unavailable(await render(data));
});
test('zero-byte package is not offered', async () => {
  const data = release(); data[0].assets[0].size = 0; unavailable(await render(data));
});
