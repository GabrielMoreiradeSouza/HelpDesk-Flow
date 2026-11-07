import { Component, input } from '@angular/core';

const paths = {
  mark: 'M4 5h7v6H4zM13 5h7v6h-7zM4 13h7v6H4zM13 16h7m-3.5-3.5v7',
  inbox: 'M4 4h16l2 10v5a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1v-5L4 4zM2 14h6l2 3h4l2-3h6',
  user: 'M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0z',
  users:
    'M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M22 21v-2a4 4 0 0 0-3-4M13 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0zM17 3a4 4 0 0 1 0 8',
  plus: 'M12 5v14M5 12h14',
  search: 'M16 10a6 6 0 1 1-12 0 6 6 0 0 1 12 0zm-1.5 4.5L20 20',
  arrow: 'M5 12h14m-6-6 6 6-6 6',
  left: 'm15 6-6 6 6 6',
  right: 'm9 6 6 6-6 6',
  down: 'm6 9 6 6 6-6',
  close: 'm6 6 12 12M6 18 18 6',
  logout: 'M10 4H5a1 1 0 0 0-1 1v14a1 1 0 0 0 1 1h5m4-13 5 5-5 5m-5-5h10',
  calendar: 'M5 5h14a1 1 0 0 1 1 1v13H4V6a1 1 0 0 1 1-1zm-1 5h16M8 3v4m8-4v4',
  list: 'M8 6h12M8 12h12M8 18h12M4 6h.01M4 12h.01M4 18h.01',
  clock: 'M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0zM12 7v5l3 2',
  check: 'm5 12 4 4L19 6',
  comment: 'M20 15a2 2 0 0 1-2 2H8l-4 3V6a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2z',
  edit: 'm13 6 5 5M4 20l4.5-1 11-11-4-4-11 11z',
};

@Component({
  selector: 'hd-icon',
  template:
    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.65" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path [attr.d]="paths[name()]"/></svg>',
  styles:
    ':host{display:inline-flex;width:1.15rem;height:1.15rem;flex:none}svg{width:100%;height:100%}',
})
export class Icon {
  readonly name = input.required<keyof typeof paths>();
  protected readonly paths = paths;
}
