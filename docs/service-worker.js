importScripts('./service-worker-assets.js');

const scope = new URL(self.registration.scope);
const prefix = 'fiziklab-static-';
const name = prefix + self.assetsManifest.version;
const shell = new URL('index.html', scope).href;
const assets = self.assetsManifest.assets.filter(({url}) =>
  new URL(url, scope).origin === scope.origin && !url.startsWith('service-worker') && !/\.(br|gz)$/.test(url));

self.addEventListener('install', event => event.waitUntil((async () => {
  const cache = await caches.open(name);
  await cache.addAll(assets.map(({url}) => new Request(new URL(url, scope), {cache:'reload'})));
  await self.skipWaiting();
})()));

self.addEventListener('activate', event => event.waitUntil((async () => {
  for (const key of await caches.keys()) {
    if (key.startsWith(prefix) && key !== name) await caches.delete(key);
  }
  await self.clients.claim();
})()));

self.addEventListener('fetch', event => {
  const request = event.request;
  const url = new URL(request.url);
  if (request.method !== 'GET' || url.origin !== scope.origin || !url.pathname.startsWith(scope.pathname)) return;
  if (request.mode === 'navigate' && [scope.pathname, new URL(shell).pathname].includes(url.pathname)) {
    event.respondWith(caches.open(name).then(cache => cache.match(shell)).then(cached => cached || fetch(request)));
  } else {
    event.respondWith(caches.open(name).then(cache => cache.match(request)).then(cached => cached || fetch(request)));
  }
});
