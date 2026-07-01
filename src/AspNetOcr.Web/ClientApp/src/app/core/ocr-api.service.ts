import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, catchError, interval, map, of, switchMap, takeWhile, tap } from 'rxjs';
import { OcrJob } from './ocr-job.model';

@Injectable({ providedIn: 'root' })
export class OcrApiService {
  private readonly http = inject(HttpClient);
  private readonly jobsSubject = new BehaviorSubject<OcrJob[]>([]);

  readonly jobs$ = this.jobsSubject.asObservable();

  refreshJobs(): Observable<OcrJob[]> {
    return this.http.get<OcrJob[]>('/api/ocr/jobs').pipe(
      tap((jobs) => this.jobsSubject.next(jobs)),
      catchError(() => of(this.jobsSubject.value))
    );
  }

  createJob(file: File): Observable<OcrJob> {
    const body = new FormData();
    body.append('file', file, file.name);
    return this.http.post<OcrJob>('/api/ocr/jobs', body).pipe(
      tap((job) => this.upsert(job))
    );
  }

  getJob(id: string): Observable<OcrJob | null> {
    return this.http.get<OcrJob>(`/api/ocr/jobs/${id}`).pipe(
      tap((job) => this.upsert(job)),
      catchError(() => of(this.jobsSubject.value.find((job) => job.id === id) ?? null))
    );
  }

  pollJob(id: string): Observable<OcrJob | null> {
    return interval(550).pipe(
      switchMap(() => this.getJob(id)),
      takeWhile((job) => job?.status === 'queued' || job?.status === 'processing', true)
    );
  }

  summary$(): Observable<{ total: number; active: number; complete: number; failed: number }> {
    return this.jobs$.pipe(
      map((jobs) => ({
        total: jobs.length,
        active: jobs.filter((job) => job.status === 'queued' || job.status === 'processing').length,
        complete: jobs.filter((job) => job.status === 'complete').length,
        failed: jobs.filter((job) => job.status === 'failed').length
      }))
    );
  }

  private upsert(job: OcrJob): void {
    const current = this.jobsSubject.value.filter((existing) => existing.id !== job.id);
    this.jobsSubject.next([job, ...current].sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc)));
  }
}
