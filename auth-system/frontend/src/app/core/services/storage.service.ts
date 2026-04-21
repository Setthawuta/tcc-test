import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class StorageService {
  private readonly storage: Storage | null =
    typeof window !== 'undefined' ? window.sessionStorage : null;

  get(key: string): string | null {
    return this.storage?.getItem(key) ?? null;
  }

  set(key: string, value: string): void {
    this.storage?.setItem(key, value);
  }

  remove(key: string): void {
    this.storage?.removeItem(key);
  }

  clear(): void {
    this.storage?.clear();
  }
}
