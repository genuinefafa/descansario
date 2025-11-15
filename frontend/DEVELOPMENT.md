# Guía de Desarrollo - Frontend

## Scripts Disponibles

### Desarrollo

```bash
npm run dev          # Iniciar servidor de desarrollo
npm run build        # Build de producción
npm run preview      # Preview del build
```

### Validación de Código

**IMPORTANTE**: Ejecuta estos comandos ANTES de hacer commit o push:

```bash
npm run validate     # Ejecuta format:check + lint + check + build (recomendado)
npm run format       # Formatear código con Prettier
npm run format:check # Verificar formato sin modificar archivos
npm run lint         # Solo ESLint
npm run lint:fix     # Auto-fix de errores de lint
npm run check        # Solo type checking con svelte-check
```

## Antes de Commitear

Siempre ejecuta:

```bash
npm run validate
```

Este comando ejecuta en orden:

1. **Format Check**: Verifica que el código esté formateado según Prettier
2. **ESLint**: Valida estilo de código y encuentra problemas
3. **Type Check**: Valida tipos con TypeScript + Svelte
4. **Build**: Asegura que el código compila

Si `npm run validate` falla, **NO** hagas commit. Arregla los errores primero.

## Configuración de Prettier

El formateo automático de código está configurado en `.prettierrc`:

- **Indentación**: 2 espacios
- **Comillas**: Single quotes (`'`)
- **Print Width**: 100 caracteres
- **Trailing Commas**: Habilitadas (estilo ES5)
- **Plugin Svelte**: Formateo específico para archivos `.svelte`

### Auto-formateo en el Editor

Configura tu editor para auto-formatear al guardar:

**VS Code**: Instala la extensión "Prettier - Code formatter" y agrega a `.vscode/settings.json`:

```json
{
  "editor.formatOnSave": true,
  "editor.defaultFormatter": "esbenp.prettier-vscode"
}
```

## Configuración de ESLint

La configuración está en `eslint.config.js` (flat config de ESLint 9).

ESLint está integrado con Prettier mediante `eslint-config-prettier`, que desactiva todas las reglas de ESLint que pueden conflictuar con Prettier.

### Reglas Principales

- TypeScript: Warnings en tipos `any` y variables sin usar
- Svelte: Validación de sintaxis y compilación
- Zero warnings policy: `--max-warnings 0`

### Archivos Ignorados

- `.svelte-kit/`
- `build/`
- `node_modules/`
- Archivos de configuración (`*.config.js`, `*.config.ts`)

## Svelte 5 Runes

ESLint está configurado para Svelte 5 con runes (`$state`, `$props`, `$effect`, etc.).

Los props destructurados se marcan como "no usados" por ESLint base, pero están deshabilitados en archivos `.svelte` porque son usados implícitamente por el compilador.

## Troubleshooting

### Error: "Type X is not assignable to type Y"

- Ejecuta `npm run check` para ver el error completo
- Revisa los tipos en `src/lib/types/`

### ESLint marca falsos positivos

- Para líneas específicas: `// eslint-disable-next-line rule-name`
- Para archivos: Agregar a `ignores` en `eslint.config.js`

### Build falla pero dev funciona

- El build es más estricto con tipos
- Revisa warnings en consola de `npm run dev`
- Ejecuta `npm run check` para ver errores de tipos
