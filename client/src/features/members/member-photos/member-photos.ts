import { Component, OnInit, inject, signal } from '@angular/core';
import { Member, Photo } from '../../../types/member';

import { AccountService } from '../../../core/services/account.service';
import { ActivatedRoute } from '@angular/router';
import { ImageUpload } from '../../../shared/image-upload/image-upload';
import { MemberService } from '../../../core/services/member.service';
import { PhotoProfileDeleteButton } from '../../../shared/photo-profile-delete-button/photo-profile-delete-button';
import { StarFavButton } from '../../../shared/star-fav-button/star-fav-button';
import { User } from '../../../types/user';

@Component({
  selector: 'app-member-photos',
  imports: [ImageUpload, StarFavButton, PhotoProfileDeleteButton],
  templateUrl: './member-photos.html',
  styleUrl: './member-photos.css',
})
export class MemberPhotos implements OnInit {
  private route = inject(ActivatedRoute);
  protected accountService = inject(AccountService);
  protected loading = signal(false);
  protected memberService = inject(MemberService);
  protected photos = signal<Photo[]>([]);

  ngOnInit(): void {
    const memberId = this.route.parent?.snapshot.paramMap.get('id');
    if (memberId) {
      this.memberService.getMemberPhotos(memberId).subscribe({
        next: (photos) => this.photos.set(photos),
      });
    }
  }

  deletePhoto(photoId: number) {
    this.memberService.deletePhoto(photoId).subscribe({
      next: () => {
        this.photos.update((photos) => photos.filter((x) => x.id !== photoId));
      },
    });
  }

  onUploadImage(file: File) {
    this.loading.set(true);
    this.memberService.uploadPhoto(file).subscribe({
      next: (photo) => {
        this.memberService.editMode.set(false);
        this.loading.set(false);
        this.photos.update((photos) => [...photos, photo]);
        if (!this.memberService.member()?.imageUrl) {
          this.setMainLocalPhoto(photo);
        }
      },
      error: (error) => {
        console.log('Error uploading image: ', error);
        this.loading.set(false);
      },
    });
  }

  setMainPhoto(photo: Photo) {
    this.memberService.setMainPhoto(photo).subscribe({
      next: () => {
        this.setMainLocalPhoto(photo);
      },
    });
  }

  private setMainLocalPhoto(photo: Photo) {
    const currentUser = this.accountService.currentUser();
    if (currentUser) currentUser.imageUrl = photo.url;
    this.accountService.setCurrentUser(currentUser as User);
    this.memberService.member.update(
      (member) =>
        ({
          ...member,
          imageUrl: photo.url,
        }) as Member,
    );
  }
}
