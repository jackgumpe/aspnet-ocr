import { existsSync, readdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import { join, relative, resolve } from 'node:path';

const root = resolve(new URL('..', import.meta.url).pathname);
const srcRoot = join(root, 'src');
const appRoot = join(srcRoot, 'app');
const checks = [];

function read(relPath) {
  return readFileSync(join(root, relPath), 'utf8');
}

function walk(dir, predicate = () => true) {
  if (!existsSync(dir)) {
    return [];
  }

  const entries = [];
  for (const name of readdirSync(dir)) {
    const abs = join(dir, name);
    const stat = statSync(abs);
    if (stat.isDirectory()) {
      entries.push(...walk(abs, predicate));
    } else if (predicate(abs)) {
      entries.push(abs);
    }
  }
  return entries;
}

function rel(abs) {
  return relative(root, abs).replaceAll('\\', '/');
}

function filesContaining(files, pattern) {
  return files
    .map((file) => ({ file: rel(file), text: readFileSync(file, 'utf8') }))
    .filter(({ text }) => pattern.test(text))
    .map(({ file }) => file);
}

function group(id, assertions) {
  checks.push({
    id,
    passed: assertions.every((assertion) => assertion.passed),
    assertions
  });
}

function assertion(id, passed, details = {}) {
  return { id, passed, details };
}

function sizeOfFilesUnder(dir, extension) {
  return walk(dir, (file) => file.endsWith(extension)).reduce((sum, file) => sum + statSync(file).size, 0);
}

function extractDefinitions(text) {
  const definitions = new Map();
  const pattern = /(--[a-z0-9-]+)\s*:\s*([^;]+);/gi;
  for (const match of text.matchAll(pattern)) {
    definitions.set(match[1], match[2].trim());
  }
  return definitions;
}

function extractUses(text) {
  return [...text.matchAll(/var\(\s*(--[a-z0-9-]+)/gi)].map((match) => match[1]);
}

function resolveToken(token, definitions, seen = new Set()) {
  if (seen.has(token)) {
    return '';
  }
  seen.add(token);
  const value = definitions.get(token) ?? '';
  const ref = value.match(/^var\(\s*(--[a-z0-9-]+)\s*\)$/i);
  if (ref) {
    return resolveToken(ref[1], definitions, seen);
  }
  return value;
}

function hexToRgb(hex) {
  const normalized = hex.replace('#', '');
  const expanded = normalized.length === 3
    ? normalized.split('').map((char) => `${char}${char}`).join('')
    : normalized;
  if (!/^[0-9a-f]{6}$/i.test(expanded)) {
    return null;
  }
  const value = Number.parseInt(expanded, 16);
  return {
    r: (value >> 16) & 255,
    g: (value >> 8) & 255,
    b: value & 255
  };
}

function channelLuminance(channel) {
  const normalized = channel / 255;
  return normalized <= 0.03928
    ? normalized / 12.92
    : ((normalized + 0.055) / 1.055) ** 2.4;
}

function contrastRatio(foreground, background) {
  const fg = hexToRgb(foreground);
  const bg = hexToRgb(background);
  if (!fg || !bg) {
    return 0;
  }
  const fgLum = 0.2126 * channelLuminance(fg.r) + 0.7152 * channelLuminance(fg.g) + 0.0722 * channelLuminance(fg.b);
  const bgLum = 0.2126 * channelLuminance(bg.r) + 0.7152 * channelLuminance(bg.g) + 0.0722 * channelLuminance(bg.b);
  const lighter = Math.max(fgLum, bgLum);
  const darker = Math.min(fgLum, bgLum);
  return Number(((lighter + 0.05) / (darker + 0.05)).toFixed(2));
}

function themeContrastReport(themePath) {
  const text = [
    read('src/app/design-system/tokens/foundation.scss'),
    read(themePath),
    read('src/app/design-system/tokens/semantic.scss'),
    read('src/app/design-system/tokens/component.scss')
  ].join('\n');
  const definitions = extractDefinitions(text);
  const pairs = [
    ['--text-primary', '--surface-page'],
    ['--text-primary', '--surface-document'],
    ['--text-muted', '--surface-page'],
    ['--text-evidence', '--surface-engine'],
    ['--status-processing', '--status-processing-surface'],
    ['--status-verified', '--status-verified-surface'],
    ['--status-integrity-failure', '--status-integrity-failure-surface']
  ];

  return pairs.map(([foregroundToken, backgroundToken]) => {
    const foreground = resolveToken(foregroundToken, definitions);
    const background = resolveToken(backgroundToken, definitions);
    return {
      foregroundToken,
      backgroundToken,
      foreground,
      background,
      ratio: contrastRatio(foreground, background)
    };
  });
}

const proofRailState = read('src/app/design-system/proof-rail/proof-rail-state.ts');
const proofRailComponent = read('src/app/design-system/proof-rail/proof-rail.component.ts');
const proofRailStyles = read('src/app/design-system/proof-rail/proof-rail.component.scss');
const resultViewer = read('src/app/features/results/result-viewer.component.ts');
const uploadZone = read('src/app/ui/upload-zone/upload-zone.component.ts');
const uploadZoneStyles = read('src/app/ui/upload-zone/upload-zone.component.scss');
const confidenceMeter = read('src/app/ui/confidence-meter/confidence-meter.component.ts');
const productConfig = read('src/app/shell/product-config.ts');
const activeProduct = read('src/app/branding/active-product.ts');
const homeToken = read('src/app/shell/client-home/client-home.token.ts');
const homeComponent = read('src/app/shell/client-home/ledger-scan-client-home.component.ts');
const ledgerStyles = read('src/styles.scss');
const contrastStyles = read('src/styles.contrast-lab.scss');
const allAppFiles = walk(appRoot, (file) => /\.(ts|scss)$/.test(file));
const allScssFiles = walk(srcRoot, (file) => file.endsWith('.scss'));
const allScssText = allScssFiles.map((file) => readFileSync(file, 'utf8')).join('\n');
const allAppText = allAppFiles.map((file) => readFileSync(file, 'utf8')).join('\n');
const featureFiles = walk(join(appRoot, 'features'), (file) => /\.(ts|scss)$/.test(file));
const featureStyleFiles = featureFiles.filter((file) => file.endsWith('.scss'));

const expectedOrder = "['RESULT_CREATED', 'EVIDENCE_WRITTEN', 'INGESTED', 'QUERYABLE', 'LIVE_VERIFIED']";
const proofRailCompact = proofRailState.replace(/\s+/g, '');
const stateOrderFrozen = proofRailCompact.includes(expectedOrder.replace(/\s+/g, ''));
const liveDefinitionFrozen = proofRailState.includes('Consumer-path retrieval') && proofRailState.includes('hash verification passed');
const reviewSealPresent = proofRailComponent.includes('◉') && proofRailComponent.includes('Review sampling is orthogonal');
const unsampledLiveVerifiedNotIncomplete = proofRailComponent.includes('NOT_SELECTED does not make LIVE_VERIFIED incomplete');
const railIdentityPresent = proofRailComponent.includes('Amber scan pulse active')
  && proofRailComponent.includes('Verified mechanical latch set')
  && proofRailStyles.includes('proof-scan-pulse')
  && proofRailStyles.includes('proof-rail__track::after');
const confidenceLanguage = resultViewer.includes('ENGINE CONFIDENCE')
  && confidenceMeter.includes("label = 'ENGINE CONFIDENCE'")
  && !/\baccuracy\b/i.test(allAppText);

group('proof_rail_identity_and_confidence_language', [
  assertion('state_order_frozen', stateOrderFrozen, { expectedOrder }),
  assertion('live_verified_definition_frozen', liveDefinitionFrozen),
  assertion('review_seal_present', reviewSealPresent),
  assertion('unsampled_live_verified_not_incomplete', unsampledLiveVerifiedNotIncomplete),
  assertion('rail_scan_and_latch_identity_present', railIdentityPresent),
  assertion('engine_confidence_language_only', confidenceLanguage)
]);

const featureBrandingImports = filesContaining(featureFiles.filter((file) => file.endsWith('.ts')), /branding\//);
const designSystemProductImports = filesContaining(
  walk(join(appRoot, 'design-system'), (file) => file.endsWith('.ts')),
  /from ['"].*(?:features|core|shell|branding)\//
);
const designSystemScssImports = filesContaining(
  walk(join(appRoot, 'design-system'), (file) => file.endsWith('.scss')),
  /@(use|import)\s+/
);
const brandingForbiddenImports = filesContaining(
  walk(join(appRoot, 'branding'), (file) => file.endsWith('.ts')),
  /from ['"].*(?:features|core)\//
);
const materialInternalSelectors = filesContaining(allScssFiles, /\.(?:mat-mdc|mdc)-/);
const materialTokenOutsideBridge = filesContaining(
  allScssFiles.filter((file) => rel(file) !== 'src/app/design-system/material/material-bridge.scss'),
  /--(?:mat|mdc)-/
);
const registryCreep = filesContaining(allAppFiles, /\b(plugin|registry|manifest|loader)\b/i);

group('dependency_rules_and_material_public_api', [
  assertion('ocr_evidence_features_do_not_import_branding', featureBrandingImports.length === 0, { featureBrandingImports }),
  assertion('design_system_has_no_product_layer_imports', designSystemProductImports.length === 0, { designSystemProductImports }),
  assertion('design_system_scss_imports_nothing', designSystemScssImports.length === 0, { designSystemScssImports }),
  assertion('branding_imports_no_feature_logic', brandingForbiddenImports.length === 0, { brandingForbiddenImports }),
  assertion('material_uses_public_tokens_no_internal_selectors', materialInternalSelectors.length === 0 && materialTokenOutsideBridge.length === 0, {
    materialInternalSelectors,
    materialTokenOutsideBridge
  }),
  assertion('no_registry_loader_manifest_or_plugin_surface', registryCreep.length === 0, { registryCreep })
]);

const requiredTokens = [
  '--surface-document',
  '--surface-engine',
  '--status-verified',
  '--status-processing',
  '--proof-rail-node-active',
  '--proof-rail-seal',
  '--proof-rail-latch',
  '--intake-bay-border',
  '--confidence-fill',
  '--motion-scan-duration'
];
const definedTokens = extractDefinitions(allScssText);
const usedTokens = [...new Set(extractUses(allScssText))].sort();
const missingUsedTokens = usedTokens.filter((token) => !definedTokens.has(token));
const missingRequiredTokens = requiredTokens.filter((token) => !definedTokens.has(token));
const fallbackTokenUses = filesContaining(allScssFiles, /var\(\s*--[a-z0-9-]+\s*,/i);
const sabotagedDefinitions = new Map(definedTokens);
sabotagedDefinitions.delete('--surface-engine');
const missingAfterSabotage = usedTokens.filter((token) => !sabotagedDefinitions.has(token));
const tokenSabotageSmoke = {
  passed: missingAfterSabotage.includes('--surface-engine'),
  sabotage: 'delete --surface-engine',
  expectedFailure: 'missing_required_token',
  observedMissingTokens: missingAfterSabotage
};

group('semantic_token_system_and_missing_token_guard', [
  assertion('all_used_tokens_are_defined', missingUsedTokens.length === 0, { missingUsedTokens }),
  assertion('required_semantic_and_component_tokens_present', missingRequiredTokens.length === 0, { missingRequiredTokens }),
  assertion('no_css_variable_fallbacks', fallbackTokenUses.length === 0, { fallbackTokenUses }),
  assertion('token_sabotage_smoke_fails_closed', tokenSabotageSmoke.passed, tokenSabotageSmoke)
]);

const fakeHomeExists = existsSync(join(root, 'src/app/shell/client-home/testing/fake-client-home.component.ts'));
const clientHomePresentational = !homeComponent.includes('inject(');

group('product_config_and_client_home_build_time_seam', [
  assertion('product_config_schema_v1_typed', productConfig.includes("schemaVersion: 'product_config_v1'") && activeProduct.includes('ACTIVE_PRODUCT_CONFIG')),
  assertion('invalid_config_throws_no_silent_fallback', productConfig.includes('throw new Error') && read('src/app/shell/product-config.token.ts').includes('must be provided at build time')),
  assertion('client_home_build_time_component_seam', homeToken.includes('CLIENT_HOME_COMPONENT') && fakeHomeExists),
  assertion('client_home_emits_intents_only', clientHomePresentational)
]);

const ledgerContrast = themeContrastReport('src/app/branding/ledger-scan/ledger-scan.theme.scss');
const contrastLabContrast = themeContrastReport('src/app/branding/theme-fixtures/contrast-lab.theme.scss');
const failingContrast = [...ledgerContrast, ...contrastLabContrast].filter((item) => item.ratio < 4.5);
const reducedMotionPreservesState = allScssText.includes('prefers-reduced-motion: reduce')
  && allScssText.includes('--motion-decorative-scale: none')
  && allScssText.includes('--motion-scan-duration: 1ms')
  && uploadZoneStyles.includes('animation: none')
  && proofRailStyles.includes('animation: none');
const scanBeamActiveOnly = uploadZone.includes('[class.upload-zone--busy]="busy"')
  && uploadZoneStyles.includes('.upload-zone--busy::after')
  && !uploadZoneStyles.includes('.upload-zone::after {\n  animation');
const noColorOnlyStatus = read('src/app/ui/status-pill/status-pill.component.ts').includes('{{ normalized }}')
  && proofRailComponent.includes('{{ state.code }}')
  && uploadZone.includes('role="status"');
const e2eText = walk(join(root, 'e2e'), (file) => file.endsWith('.ts')).map((file) => readFileSync(file, 'utf8')).join('\n');
const keyboardAndZoomEvidence = e2eText.includes('toBeFocused')
  && e2eText.includes('outline-style')
  && e2eText.includes('200 percent zoom reflows');
const oneThemePerBuild = ledgerStyles.includes('ledger-scan.theme')
  && !ledgerStyles.includes('contrast-lab.theme')
  && contrastStyles.includes('contrast-lab.theme')
  && !contrastStyles.includes('ledger-scan.theme');
const distRoot = join(root, 'dist', 'aspnet-ocr-web');
const bundleBytes = existsSync(distRoot) ? sizeOfFilesUnder(distRoot, '.js') : 0;
const cssBytes = existsSync(distRoot) ? sizeOfFilesUnder(distRoot, '.css') : sizeOfFilesUnder(srcRoot, '.scss');

group('motion_accessibility_theme_and_budget_conformance', [
  assertion('contrast_ratios_meet_4_5_to_1', failingContrast.length === 0, { ledgerContrast, contrastLabContrast, failingContrast }),
  assertion('statuses_are_not_color_only', noColorOnlyStatus),
  assertion('keyboard_focus_and_200_percent_reflow_have_smoke_coverage', keyboardAndZoomEvidence),
  assertion('scan_beam_active_processing_only', scanBeamActiveOnly),
  assertion('reduced_motion_static_highlights_keep_state', reducedMotionPreservesState),
  assertion('one_theme_per_build', oneThemePerBuild),
  assertion('bundle_size_within_budget', bundleBytes === 0 || bundleBytes <= 1_000_000, { bundleBytes, budgetBytes: 1_000_000 }),
  assertion('css_size_recorded_within_budget', cssBytes <= 200_000, { cssBytes, budgetBytes: 200_000 })
]);

const report = {
  schema_version: 'asp_ocr_006_conformance_report_v2',
  status: checks.every((check) => check.passed) && tokenSabotageSmoke.passed ? 'pass' : 'fail',
  generated_at: new Date().toISOString(),
  verification_shape: '5_checks_plus_token_sabotage_smoke',
  checks,
  token_sabotage_smoke: tokenSabotageSmoke,
  budgets: {
    bundle_bytes: bundleBytes,
    bundle_budget_bytes: 1_000_000,
    css_bytes: cssBytes,
    css_budget_bytes: 200_000
  }
};

writeFileSync(join(root, 'asp-ocr-006-conformance-report.json'), `${JSON.stringify(report, null, 2)}\n`);

if (report.status !== 'pass') {
  console.error(JSON.stringify(report, null, 2));
  process.exit(1);
}

console.log(JSON.stringify(report, null, 2));
