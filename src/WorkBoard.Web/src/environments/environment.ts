/**
 * Build-time configuration — the **production** values.
 *
 * `angular.json` swaps this file for `environment.development.ts` when building with the
 * `development` configuration (`ng serve`, `ng build --configuration development`), so nothing in
 * the app ever imports the development file directly.
 *
 * ## How this relates to `config.json`
 *
 * These values are the *default*. At startup `RuntimeConfigService` fetches `config.json` and, if it
 * carries a non-empty value, that wins. Both layers exist because they solve different problems:
 *
 * | Layer | Decided at | Used by |
 * | --- | --- | --- |
 * | `environment.ts` | build time | anyone running `ng build` directly |
 * | `config.json` | container start | the Docker image, written by `docker/web-entrypoint.sh` |
 *
 * Without the runtime layer, the API URL would be compiled into the bundle, so staging and
 * production would need separate images — and the artefact reaching production would not be the one
 * that was tested. Without the build-time layer, a plain `ng build` would have no sensible default.
 *
 * Production leaves `apiBaseUrl` empty on purpose: a deployed image is always configured by
 * `config.json`, and an empty string makes a missing configuration fail loudly at the first request
 * rather than silently calling localhost.
 */
export const environment = {
  production: true,
  apiBaseUrl: '',
};
