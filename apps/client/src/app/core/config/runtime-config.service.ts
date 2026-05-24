import { Injectable } from '@angular/core';

export interface RuntimeConfig {
  apiBaseUrl: string;
}

@Injectable({ providedIn: 'root' })
export class RuntimeConfigService {
  private config: RuntimeConfig | null = null;

  async load(): Promise<void> {
    const response = await fetch('/assets/runtime-config.json', {
      cache: 'no-store',
    });

    if (!response.ok) {
      throw new Error(`Failed to load runtime config: HTTP ${response.status}`);
    }

    const payload = (await response.json()) as Partial<RuntimeConfig>;
    const apiBaseUrl = payload.apiBaseUrl?.trim().replace(/\/+$/, '');

    if (!apiBaseUrl) {
      throw new Error('Runtime config is missing a valid apiBaseUrl value.');
    }

    this.config = {
      apiBaseUrl,
    };
  }

  get apiBaseUrl(): string | null {
    return this.config?.apiBaseUrl ?? null;
  }

  get requiredApiBaseUrl(): string {
    if (!this.config) {
      throw new Error('Runtime config has not been loaded yet.');
    }

    return this.config.apiBaseUrl;
  }
}
