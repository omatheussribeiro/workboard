/**
 * Build-time configuration — the **development** values.
 *
 * `angular.json` substitutes this file for `environment.ts` under the `development` configuration,
 * which is what `ng serve` and `npm start` use.
 *
 * ## Why `apiBaseUrl` is empty here
 *
 * In development the API is reached through the dev-server proxy, not directly. `proxy.conf.json`
 * forwards `/api` to `https://localhost:7179`, and that file is the single place naming the
 * backend's address. Leaving this empty keeps the request URLs relative, so the proxy sees them.
 *
 * Two things fall out of that, and both matter on a fresh machine:
 *
 * - **No CORS.** The browser only ever talks to `http://localhost:4200`, so it is all same-origin.
 * - **The ASP.NET development certificate stops mattering.** Node terminates the TLS connection to
 *   Kestrel with certificate validation disabled (`"secure": false` in the proxy config). The
 *   browser never sees that certificate, so an untrusted one cannot fail the request.
 *
 * Setting this to `https://localhost:7179` makes the browser call the API directly and bypass the
 * proxy. That works, and it exercises the same code path as production — but it needs CORS to allow
 * `http://localhost:4200` (it does) *and* the development certificate to be trusted:
 *
 * ```bash
 * dotnet dev-certs https --trust
 * ```
 *
 * Without that, every request fails with an opaque TLS error that looks like the API is down.
 *
 * Note that `angular.json` and `proxy.conf.json` are read once, when the dev server starts. Changing
 * either — including this file's replacement rule — needs `ng serve` restarted, not just reloaded.
 */
export const environment = {
  production: false,
  apiBaseUrl: '',
};
