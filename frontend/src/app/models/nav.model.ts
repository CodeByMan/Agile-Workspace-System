export interface NavSubItem {
  name: string;
  path: string;
  new?: boolean;
  pro?: boolean;
}

export interface NavItem {
  icon: string;
  name: string;
  path?: string;
  subItems?: NavSubItem[];
}

export interface NavGroup {
  title: string;
  items: NavItem[];
}
