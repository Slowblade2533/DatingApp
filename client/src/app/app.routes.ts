import { Routes } from '@angular/router';
import { authGuard } from '../core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('../features/home/home').then((m) => m.Home),
  },
  {
    path: '',
    runGuardsAndResolvers: 'always',
    canActivate: [authGuard],
    children: [
      {
        path: 'members',
        loadComponent: () =>
          import('../features/members/member-list/member-list').then((m) => m.MemberList),
        title: 'Members',
      },
      {
        path: 'members/:id',
        loadComponent: () =>
          import('../features/members/member-detailed/member-detailed').then(
            (m) => m.MemberDetailed,
          ),
        title: 'Member Profile',
        children: [
          {
            path: '',
            redirectTo: 'profile',
            pathMatch: 'full',
          },
          {
            path: 'profile',
            loadComponent: () =>
              import('../features/members/member-profile/member-profile').then(
                (m) => m.MemberProfile,
              ),
            title: 'Profile',
          },
          {
            path: 'photos',
            loadComponent: () =>
              import('../features/members/member-photos/member-photos').then((m) => m.MemberPhotos),
            title: 'Photos',
          },
          {
            path: 'messages',
            loadComponent: () =>
              import('../features/members/member-messages/member-messages').then(
                (m) => m.MemberMessages,
              ),
            title: 'MemberMessages',
          },
        ],
      },
      {
        path: 'lists',
        loadComponent: () => import('../features/lists/lists').then((m) => m.Lists),
        title: 'Lists',
      },
      {
        path: 'messages',
        loadComponent: () => import('../features/messages/messages').then((m) => m.Messages),
        title: 'Messages',
      },
    ],
  },
  {
    path: 'errors',
    loadComponent: () =>
      import('../features/test-errors/test-errors').then((m) => m.TestErrors),
  },
  {
    path: 'server-error',
    loadComponent: () =>
      import('../shared/errors/server-error/server-error').then((m) => m.ServerError),
  },
  {
    path: '**',
    loadComponent: () =>
      import('../shared/errors/not-found/not-found').then((m) => m.NotFound),
  },
];
