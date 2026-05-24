import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { TimeoutError, catchError, throwError, timeout } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DocumentAskRequest, DocumentAskResponse } from './document-qa.models';

interface ProblemDetailsPayload {
  title?: string;
  detail?: string;
  status?: number;
}

export interface DocumentQaApiError {
  title: string;
  message: string;
  status?: number;
}

@Injectable({ providedIn: 'root' })
export class DocumentQaService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/api/qa/ask`;

  ask(request: DocumentAskRequest) {
    return this.http.post<DocumentAskResponse>(this.endpoint, request).pipe(
      timeout(95_000),
      catchError((error) => throwError(() => this.mapError(error)))
    );
  }

  private mapError(error: unknown): DocumentQaApiError {
    if (error instanceof TimeoutError) {
      return {
        title: 'Request timed out',
        message: 'The API did not answer within the expected time window. Try again in a few moments.',
      };
    }

    if (error instanceof HttpErrorResponse) {
      const problemDetails = this.readProblemDetails(error);
      const fallbackMessage = this.getFallbackMessage(error.status);

      if (error.status === 0) {
        return {
          title: 'Connection failed',
          message: 'The browser could not reach the Docquery API. Verify that the backend is running and that CORS allows the current origin.',
          status: 0,
        };
      }

      return {
        title: problemDetails.title ?? this.getFallbackTitle(error.status),
        message: problemDetails.detail ?? fallbackMessage,
        status: error.status,
      };
    }

    return {
      title: 'Unexpected error',
      message: 'The request failed before a response could be rendered. Check the browser console for more details.',
    };
  }

  private readProblemDetails(error: HttpErrorResponse): ProblemDetailsPayload {
    const payload = error.error;

    if (!payload || typeof payload !== 'object') {
      return {};
    }

    return payload as ProblemDetailsPayload;
  }

  private getFallbackTitle(status: number): string {
    return status === 401
      ? 'Access key rejected'
      : status === 413
      ? 'Document too large'
      : status === 502
        ? 'Provider authentication failed'
        : status === 503
          ? 'Provider unavailable'
          : status === 504
            ? 'Provider timeout'
            : status === 400
              ? 'Request validation failed'
              : 'Request failed';
  }

  private getFallbackMessage(status: number): string {
    switch (status) {
      case 401:
        return 'The shared access key is missing or invalid. Enter the current key again and retry the request.';
      case 400:
        return 'Both the document text and question are required before the API can process the request.';
      case 413:
        return 'The document text and question exceed the model context limit. Reduce the payload size and retry.';
      case 502:
        return 'The upstream LLM provider rejected the request. Review provider credentials and access policy.';
      case 503:
        return 'The upstream LLM provider is temporarily unavailable. Retry after a short delay.';
      case 504:
        return 'The upstream LLM provider did not finish in time. Try the request again.';
      default:
        return 'The API returned an unexpected error while processing the question.';
    }
  }
}
