import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Component, computed, inject, PLATFORM_ID, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { $t, updatePreset, updateSurfacePalette } from '@primeuix/themes';
import { PrimeNG } from 'primeng/config';
import { SelectButtonModule } from 'primeng/selectbutton';
import { Button } from 'primeng/button';
import { brandGreenPalette, colors, presets, surfaces } from '@/core/constants/config';
import { KeyOfType, SurfacesType } from '@/core/models/layout';
import { LayoutService } from '@/core/services/layout';
import { PresetKey } from '@/core/models/common';

@Component({
    selector: 'app-configurator',
    imports: [CommonModule, FormsModule, SelectButtonModule, Button],
    templateUrl: './configurator.html',
    styleUrl: './configurator.scss'
})
export class Configurator {
    router = inject(Router);

    config: PrimeNG = inject(PrimeNG);

    layoutService: LayoutService = inject(LayoutService);

    platformId = inject(PLATFORM_ID);

    primeng = inject(PrimeNG);

    presetOptions = [
        { label: 'Hiện đại', value: 'Aura' },
        { label: 'Thanh lịch', value: 'Lara' },
        { label: 'Tối giản', value: 'Nora' }
    ] satisfies ReadonlyArray<{ label: string; value: PresetKey }>;

    menuModeOptions = [
        { label: 'Cố định', value: 'static' },
        { label: 'Trượt đè', value: 'overlay' }
    ];

    surfaces = surfaces;

    brandGreenPalette = brandGreenPalette;

    showMenuModeButton = signal(!this.router.url.includes('auth'));

    isDarkTheme = computed(() => this.layoutService.layoutConfig().darkTheme);

    selectedPrimaryColor = computed(() => {
        return this.layoutService.layoutConfig().primary;
    });

    selectedSurfaceColor = computed(() => this.layoutService.layoutConfig().surface);

    selectedPreset = computed(() => this.layoutService.layoutConfig().preset);

    selectedMenuMode = computed(() => this.layoutService.layoutConfig().menuMode);

    menuMode = computed(() => this.layoutService.layoutConfig().menuMode);

    primaryColors = computed<SurfacesType[]>(() => {
        const presetKey = this.layoutService.layoutConfig().preset as PresetKey;
        const presetPalette = presets[presetKey].primitive;

        const palettes: SurfacesType[] = [{ name: 'brand', palette: this.brandGreenPalette }];

        colors.forEach((color) => {
            const palette = presetPalette?.[color as KeyOfType<typeof presetPalette>];
            if (palette) {
                palettes.push({
                    name: color,
                    palette: palette as SurfacesType['palette']
                });
            }
        });

        return palettes;
    });

    ngOnInit() {
        if (isPlatformBrowser(this.platformId)) {
            this.onPresetChange(this.layoutService.layoutConfig().preset as PresetKey);
        }
    }

    getPresetExt() {
        const color: SurfacesType = this.primaryColors().find((c) => c.name === this.selectedPrimaryColor()) || {};
        const preset = this.layoutService.layoutConfig().preset;

        if (color.name === 'noir') {
            return {
                semantic: {
                    primary: {
                        50: '{surface.50}',
                        100: '{surface.100}',
                        200: '{surface.200}',
                        300: '{surface.300}',
                        400: '{surface.400}',
                        500: '{surface.500}',
                        600: '{surface.600}',
                        700: '{surface.700}',
                        800: '{surface.800}',
                        900: '{surface.900}',
                        950: '{surface.950}'
                    },
                    colorScheme: {
                        light: {
                            primary: {
                                color: '{primary.950}',
                                contrastColor: '#ffffff',
                                hoverColor: '{primary.800}',
                                activeColor: '{primary.700}'
                            },
                            highlight: {
                                background: '{primary.950}',
                                focusBackground: '{primary.700}',
                                color: '#ffffff',
                                focusColor: '#ffffff'
                            }
                        },
                        dark: {
                            primary: {
                                color: '{primary.50}',
                                contrastColor: '{primary.950}',
                                hoverColor: '{primary.200}',
                                activeColor: '{primary.300}'
                            },
                            highlight: {
                                background: '{primary.50}',
                                focusBackground: '{primary.300}',
                                color: '{primary.950}',
                                focusColor: '{primary.950}'
                            }
                        }
                    }
                }
            };
        } else {
            if (preset === 'Nora') {
                return {
                    semantic: {
                        primary: color.palette,
                        colorScheme: {
                            light: {
                                primary: {
                                    color: '{primary.600}',
                                    contrastColor: '#ffffff',
                                    hoverColor: '{primary.700}',
                                    activeColor: '{primary.800}'
                                },
                                highlight: {
                                    background: '{primary.600}',
                                    focusBackground: '{primary.700}',
                                    color: '#ffffff',
                                    focusColor: '#ffffff'
                                }
                            },
                            dark: {
                                primary: {
                                    color: '{primary.500}',
                                    contrastColor: '{surface.900}',
                                    hoverColor: '{primary.400}',
                                    activeColor: '{primary.300}'
                                },
                                highlight: {
                                    background: '{primary.500}',
                                    focusBackground: '{primary.400}',
                                    color: '{surface.900}',
                                    focusColor: '{surface.900}'
                                }
                            }
                        }
                    }
                };
            } else {
                return {
                    semantic: {
                        primary: color.palette,
                        colorScheme: {
                            light: {
                                primary: {
                                    color: '{primary.500}',
                                    contrastColor: '#ffffff',
                                    hoverColor: '{primary.600}',
                                    activeColor: '{primary.700}'
                                },
                                highlight: {
                                    background: '{primary.50}',
                                    focusBackground: '{primary.100}',
                                    color: '{primary.700}',
                                    focusColor: '{primary.800}'
                                }
                            },
                            dark: {
                                primary: {
                                    color: '{primary.400}',
                                    contrastColor: '{surface.900}',
                                    hoverColor: '{primary.300}',
                                    activeColor: '{primary.200}'
                                },
                                highlight: {
                                    background: 'color-mix(in srgb, {primary.400}, transparent 84%)',
                                    focusBackground: 'color-mix(in srgb, {primary.400}, transparent 76%)',
                                    color: 'rgba(255,255,255,.87)',
                                    focusColor: 'rgba(255,255,255,.87)'
                                }
                            }
                        }
                    }
                };
            }
        }
    }

    updateColors(event: any, type: string, color: any) {
        if (type === 'primary') {
            this.layoutService.layoutConfig.update((state) => ({ ...state, primary: color.name }));
        } else if (type === 'surface') {
            this.layoutService.layoutConfig.update((state) => ({ ...state, surface: color.name }));
        }
        this.applyTheme(type, color);

        event.stopPropagation();
    }

    applyTheme(type: string, color: any) {
        if (type === 'primary') {
            updatePreset(this.getPresetExt());
        } else if (type === 'surface') {
            updateSurfacePalette(color.palette);
        }
    }

    onPresetChange(presetKey: PresetKey) {
        // update state
        this.layoutService.layoutConfig.update((state) => ({
            ...state,
            preset: presetKey
        }));

        // get preset config
        const preset = presets[presetKey];

        // current surface palette
        const surfacePalette = this.surfaces.find((s) => s.name === this.selectedSurfaceColor())?.palette;

        // apply theme
        $t().preset(preset).preset(this.getPresetExt()).surfacePalette(surfacePalette).use({ useDefaultOptions: true });
    }

    onMenuModeChange(event: string) {
        this.layoutService.layoutConfig.update((prev) => ({ ...prev, menuMode: event }));
    }

    toggleDarkMode() {
        this.layoutService.layoutConfig.update((state) => ({ ...state, darkTheme: !state.darkTheme }));
    }
}
