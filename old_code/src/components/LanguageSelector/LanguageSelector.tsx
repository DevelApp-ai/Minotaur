import React from 'react';
import { useTranslation } from 'react-i18next';
import './LanguageSelector.css';

const languages = [
  { code: 'en', name: 'English', flag: '🇺🇸' },
  { code: 'es', name: 'Español', flag: '🇪🇸' },
  { code: 'fr', name: 'Français', flag: '🇫🇷' },
  { code: 'de', name: 'Deutsch', flag: '🇩🇪' },
  { code: 'ja', name: '日本語', flag: '🇯🇵' },
  { code: 'pt-BR', name: 'Português (BR)', flag: '🇧🇷' },
  { code: 'nl', name: 'Nederlands', flag: '🇳🇱' },
  { code: 'ne', name: 'नेपाली', flag: '🇳🇵' },
  { code: 'da', name: 'Dansk', flag: '🇩🇰' },
  { code: 'no', name: 'Norsk', flag: '🇳🇴' },
  { code: 'sv', name: 'Svenska', flag: '🇸🇪' },
];

export const LanguageSelector: React.FC = () => {
  const { i18n, t } = useTranslation();

  const handleLanguageChange = (languageCode: string) => {
    i18n.changeLanguage(languageCode);
  };

  return (
    <div className="language-selector">
      <label htmlFor="language-select" className="language-label">
        {t('language.selector')}:
      </label>
      <select
        id="language-select"
        value={i18n.language}
        onChange={(e) => handleLanguageChange(e.target.value)}
        className="language-select"
      >
        {languages.map((lang) => (
          <option key={lang.code} value={lang.code}>
            {lang.flag} {lang.name}
          </option>
        ))}
      </select>
    </div>
  );
};

