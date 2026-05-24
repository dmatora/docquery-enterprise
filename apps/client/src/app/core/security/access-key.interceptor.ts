import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AccessKeyService } from './access-key.service';

export const accessKeyInterceptor: HttpInterceptorFn = (request, next) => {
  const accessKeyService = inject(AccessKeyService);
  const accessKey = accessKeyService.accessKey();
  const isApiRequest = request.url.startsWith(environment.apiBaseUrl);

  const authorizedRequest = isApiRequest && accessKey
    ? request.clone({
        setHeaders: {
          'X-Api-Access-Key': accessKey,
        },
      })
    : request;

  return next(authorizedRequest).pipe(
    catchError((error) => {
      if (isApiRequest && error instanceof HttpErrorResponse && error.status === 401) {
        accessKeyService.clearAccessKey();
      }

      return throwError(() => error);
    })
  );
};
