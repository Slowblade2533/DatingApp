import { EditableMember, Member, Photo } from '../../types/member';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { PaginatedResult, PaginationHeader } from '../../types/pagination';
import { map, tap } from 'rxjs';

import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class MemberService {
  private baseUrl = environment.apiUrl;
  private http = inject(HttpClient);

  editMode = signal(false);
  member = signal<Member | null>(null);

  deletePhoto(photoId: number) {
    return this.http.delete(this.baseUrl + 'members/delete-photo/' + photoId);
  }

  getMember(id: string) {
    return this.http.get<Member>(this.baseUrl + 'members/' + id).pipe(
      tap((member) => {
        this.member.set(member);
      }),
    );
  }

  getMemberPhotos(id: string) {
    return this.http.get<Photo[]>(this.baseUrl + 'members/' + id + '/photos');
  }

  getMembers(pageNumber: number, pageSize: number) {
    // ใส่ pageNumber และ pageSize เป็น Query String (?pageNumber=1&pageSize=10)
    const params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);

    return this.http
      .get<Member[]>(this.baseUrl + 'members', {
        observe: 'response', // 👈 ขอดู Response เต็ม ไม่ใช่แค่ body เพื่อจะได้อ่าน Header ได้
        params,
      })
      .pipe(
        map((response) => {
          // ดึง Pagination จาก Header
          const paginationHeader = response.headers.get('Pagination');
          let paginationData: PaginationHeader = {
            currentPage: 1,
            itemsPerPage: pageSize,
            totalItems: 0,
            totalPages: 0,
          };
          if (paginationHeader) {
            try {
              paginationData = JSON.parse(paginationHeader);
            } catch (error) {
              console.error('Failed to parse pagination header', error);
            }
          }
          const result: PaginatedResult<Member[]> = {
            items: response.body ?? [],
            pagination: paginationData,
          };

          return result;
        }),
      );
  }

  setMainPhoto(photo: Photo) {
    return this.http.put(this.baseUrl + 'members/set-main-photo/' + photo.id, {});
  }

  updateMember(member: EditableMember) {
    return this.http.put(this.baseUrl + 'members', member);
  }

  uploadPhoto(file: File) {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<Photo>(this.baseUrl + 'members/add-photo', formData);
  }
}
