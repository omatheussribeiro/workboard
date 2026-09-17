/**
 * An RFC 9457 error response.
 *
 * Every failure from the API arrives in this shape, which is what lets one interceptor and one toast
 * handle all of them. `correlationId` is the value to quote in a bug report: it joins the response to
 * the server logs for that request.
 */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  correlationId?: string;
  errors?: Record<string, string[]>;
}
