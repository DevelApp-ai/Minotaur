import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';

// Import translation files
import enTranslations from '../locales/en/translation.json';
import esTranslations from '../locales/es/translation.json';
import frTranslations from '../locales/fr/translation.json';
import deTranslations from '../locales/de/translation.json';
import jaTranslations from '../locales/ja/translation.json';
import ptBRTranslations from '../locales/pt-BR/translation.json';
import nlTranslations from '../locales/nl/translation.json';
import neTranslations from '../locales/ne/translation.json';
import daTranslations from '../locales/da/translation.json';
import noTranslations from '../locales/no/translation.json';
import svTranslations from '../locales/sv/translation.json';

const resources = {
  en: {
    translation: enTranslations,
  },
  es: {
    translation: esTranslations,
  },
  fr: {
    translation: frTranslations,
  },
  de: {
    translation: deTranslations,
  },
  ja: {
    translation: jaTranslations,
  },
  'pt-BR': {
    translation: ptBRTranslations,
  },
  nl: {
    translation: nlTranslations,
  },
  ne: {
    translation: neTranslations,
  },
  da: {
    translation: daTranslations,
  },
  no: {
    translation: noTranslations,
  },
  sv: {
    translation: svTranslations,
  },
};

i18n
  // Detect user language
  .use(LanguageDetector)
  // Pass the i18n instance to react-i18next
  .use(initReactI18next)
  // Initialize i18next
  .init({
    resources,
    fallbackLng: 'en',
    debug: process.env.NODE_ENV === 'development',

    interpolation: {
      escapeValue: false, // React already does escaping
    },

    detection: {
      order: ['localStorage', 'navigator', 'htmlTag'],
      caches: ['localStorage'],
    },

    react: {
      useSuspense: false,
    },
  });

export default i18n;

