'use strict';
(async () => {
  const list = document.getElementById('release-files');
  const status = document.getElementById('release-status');
  try {
    const response = await fetch('https://api.github.com/repos/jakeharvey162-source/skyline-rush-ffp/releases?per_page=20', {signal: AbortSignal.timeout(10000)});
    if (!response.ok) throw new Error('Release service unavailable');
    const releases = await response.json();
    const release = releases.find(r => !r.draft && r.assets.some(a => /^skyline-rush-windows\.7z\.001$/.test(a.name)));
    if (!release) throw new Error('No public package yet');
    const files = release.assets.filter(a => /^skyline-rush-windows\.7z\.\d{3}$/.test(a.name)).sort((a,b) => a.name.localeCompare(b.name));
    list.replaceChildren();
    files.forEach((file, index) => {
      const url = new URL(file.browser_download_url);
      if (url.origin !== 'https://github.com' || !url.pathname.startsWith('/jakeharvey162-source/skyline-rush-ffp/releases/download/')) throw new Error('Unexpected download location');
      const link = document.createElement('a');
      link.className = 'button download-link';
      link.href = url.href;
      link.textContent = 'Download part ' + (index + 1) + ' of ' + files.length + ' · ' + (file.size / 1048576).toFixed(0) + ' MB';
      list.append(link);
    });
    const checksums = release.assets.find(a => a.name === 'SHA256SUMS.txt');
    if (checksums) {
      const url = new URL(checksums.browser_download_url);
      if (url.origin === 'https://github.com' && url.pathname.startsWith('/jakeharvey162-source/skyline-rush-ffp/releases/download/')) {
        const link = document.createElement('a'); link.className = 'secondary'; link.href = url.href; link.textContent = 'Download integrity checksums'; list.append(link);
      }
    }
    status.textContent = release.name + ' · Download every part before extracting.';
  } catch (error) {
    list.replaceChildren();
    const message = document.createElement('p');
    message.textContent = 'Download links are not available right now. Check the public releases page below.';
    list.append(message);
    status.textContent = 'No APK is available. Windows builds are published after packaging checks pass.';
  }
})();
