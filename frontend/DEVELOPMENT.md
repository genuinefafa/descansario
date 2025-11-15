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
npm run validate     # Ejecuta lint + check + build (recomendado)
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
1. **ESLint**: Valida estilo de código y encuentra problemas
2. **Type Check**: Valida tipos con TypeScript + Svelte
3. **Build**: Asegura que el código compila

Si `npm run validate` falla, **NO** hagas commit. Arregla los errores primero.

## Configuración de ESLint

La configuración está en `eslint.config.js` (flat config de ESLint 9).

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
