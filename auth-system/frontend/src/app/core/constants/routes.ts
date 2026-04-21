export const APP_ROUTES = {
  login: 'login',
  register: 'register',
  welcome: 'welcome',
} as const;

export const APP_PATHS = {
  login: `/${APP_ROUTES.login}`,
  register: `/${APP_ROUTES.register}`,
  welcome: `/${APP_ROUTES.welcome}`,
} as const;
