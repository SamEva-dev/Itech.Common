export type AuthGateUiState = 'unavailable' | 'forbidden' | 'expired' | 'loading' | 'error' | 'pending';
export type AuthGateUiLanguage = 'fr' | 'en';
export interface AuthGateUiCopy { title: string; description: string; retry: string; }
const messages: Record<AuthGateUiLanguage, Record<AuthGateUiState, AuthGateUiCopy>> = {
  fr: {
    unavailable: { title: 'Service de connexion indisponible', description: 'Impossible de joindre ou de vérifier le service AuthGate. Réessayez dans quelques instants.', retry: 'Réessayer' },
    forbidden: { title: 'Accès refusé', description: 'Votre compte ne dispose pas des habilitations nécessaires.', retry: 'Changer de compte' },
    expired: { title: 'Session expirée', description: 'Reconnectez-vous pour continuer.', retry: 'Se reconnecter' },
    loading: { title: 'Connexion en cours', description: 'Vérification de votre session sécurisée.', retry: 'Réessayer' },
    error: { title: 'Connexion impossible', description: 'La connexion sécurisée n’a pas abouti.', retry: 'Réessayer' },
    pending: { title: 'Activation en cours', description: 'Vos accès sont en cours de préparation.', retry: 'Vérifier mes accès' },
  },
  en: {
    unavailable: { title: 'Sign-in service unavailable', description: 'AuthGate could not be reached or verified. Please try again shortly.', retry: 'Try again' },
    forbidden: { title: 'Access denied', description: 'Your account does not have the required permissions.', retry: 'Switch account' },
    expired: { title: 'Session expired', description: 'Sign in again to continue.', retry: 'Sign in again' },
    loading: { title: 'Signing in', description: 'Checking your secure session.', retry: 'Try again' },
    error: { title: 'Sign-in failed', description: 'Secure sign-in could not be completed.', retry: 'Try again' },
    pending: { title: 'Activation pending', description: 'Your access is being prepared.', retry: 'Check access' },
  },
};
export function authGateUiCopy(state: AuthGateUiState, language: string = 'fr'): AuthGateUiCopy {
  return { ...messages[language.toLowerCase().split('-')[0] === 'en' ? 'en' : 'fr'][state] };
}
