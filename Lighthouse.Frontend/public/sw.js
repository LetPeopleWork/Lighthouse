// Bug #5732 — tombstone service worker.
//
// Lighthouse shipped an installable-PWA service worker until v26.3.13.16 (removed in
// 497471d38). Dropping the plugin stopped new registrations but never uninstalled the
// existing ones, so those browsers kept serving a precached March 2026 app shell — which
// broke for good once the API moved to /api/latest. This file replaces that worker and
// removes it. It registers no fetch handler, so it never serves anything from cache.
//
// Safe to delete once telemetry shows no client still requesting /sw.js.

self.addEventListener("install", () => {
	self.skipWaiting();
});

self.addEventListener("activate", (event) => {
	event.waitUntil(
		(async () => {
			const cacheKeys = await caches.keys();
			await Promise.all(cacheKeys.map((key) => caches.delete(key)));

			await self.registration.unregister();

			// The page currently on screen is still running the stale bundle; reload it so the
			// user lands on the current client instead of having to hard-refresh.
			const windowClients = await self.clients.matchAll({ type: "window" });
			for (const client of windowClients) {
				client.navigate(client.url);
			}
		})(),
	);
});
