// ***********************************************************
// This example support/index.js is processed and
// loaded automatically before your test files.
//
// This is a great place to put global configuration and
// behavior that modifies Cypress.
//
// You can change the location of this file or turn off
// automatically serving support files with the
// 'supportFile' configuration option.
//
// You can read more here:
// https://on.cypress.io/configuration
// ***********************************************************

// Import commands.js using ES2015 syntax:
import './commands'

// Alternatively you can use CommonJS syntax:
// require('./commands')

// Coleta a cobertura istanbul (window.__coverage__) instrumentada pelo vite-plugin-istanbul
// ao final de cada teste — alimenta cypress-coverage/lcov.info (ver documentation/TEST.md, seção 7).
import '@cypress/code-coverage/support'
