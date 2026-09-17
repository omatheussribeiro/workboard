// @ts-check
const eslint = require('@eslint/js');
const tseslint = require('typescript-eslint');
const angular = require('angular-eslint');

module.exports = tseslint.config(
  {
    files: ['**/*.ts'],
    extends: [
      eslint.configs.recommended,
      ...tseslint.configs.recommended,
      ...tseslint.configs.stylistic,
      ...angular.configs.tsRecommended,
    ],
    processor: angular.processInlineTemplates,
    rules: {
      // One prefix across the app, so a WorkBoard component is recognisable in any template.
      '@angular-eslint/directive-selector': [
        'error',
        { type: 'attribute', prefix: 'wb', style: 'camelCase' },
      ],
      '@angular-eslint/component-selector': [
        'error',
        { type: 'element', prefix: ['wb', 'app'], style: 'kebab-case' },
      ],

      // Signals and standalone components are the whole point of this codebase; the older APIs are
      // errors rather than warnings so they cannot creep back in.
      '@angular-eslint/prefer-standalone': 'error',
      '@angular-eslint/no-empty-lifecycle-method': 'error',

      '@typescript-eslint/no-unused-vars': [
        'error',
        { argsIgnorePattern: '^_', varsIgnorePattern: '^_' },
      ],
      '@typescript-eslint/consistent-type-imports': [
        'error',
        { prefer: 'type-imports', fixStyle: 'separate-type-imports' },
      ],
    },
  },
  {
    files: ['**/*.html'],
    extends: [...angular.configs.templateRecommended, ...angular.configs.templateAccessibility],
    rules: {
      // Accessibility rules are errors, not warnings. A warning in a template nobody reads is the
      // same as no rule at all.
      '@angular-eslint/template/label-has-associated-control': 'error',
      '@angular-eslint/template/click-events-have-key-events': 'error',
      '@angular-eslint/template/interactive-supports-focus': 'error',
    },
  },
);
