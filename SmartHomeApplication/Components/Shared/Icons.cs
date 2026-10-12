using Microsoft.AspNetCore.Components;

namespace SmartHomeApplication.Components.Shared;

public static class Icons
{
    public static RenderFragment ChevronLeft => Svg("""
        <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
            <path d="M10 12L6 8l4-4" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
        """);

    public static RenderFragment ChevronDown => Svg("""
        <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
            <path d="M3 5l4 4 4-4" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
        """);

    public static RenderFragment Zap => Svg("""
        <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
            <path d="M9 1L3 9h5l-1 6 6-8H8l1-6z" stroke="currentColor" stroke-width="1.4" stroke-linejoin="round" />
        </svg>
        """);

    public static RenderFragment Car => Svg("""
        <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
            <rect x="1" y="6" width="14" height="7" rx="1.5" stroke="currentColor" stroke-width="1.4" />
            <path d="M3.5 6L5 3h6l1.5 3" stroke="currentColor" stroke-width="1.4" stroke-linejoin="round" />
            <circle cx="4.5" cy="13" r="1.5" fill="currentColor" />
            <circle cx="11.5" cy="13" r="1.5" fill="currentColor" />
        </svg>
        """);

    public static RenderFragment Thermometer => Svg("""
        <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
            <path d="M8 2v7" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" />
            <circle cx="8" cy="11.5" r="2.5" stroke="currentColor" stroke-width="1.4" />
            <path d="M6 5h-1M6 7h-1" stroke="currentColor" stroke-width="1.2" stroke-linecap="round" />
        </svg>
        """);

    public static RenderFragment Dish => Svg("""
        <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
            <ellipse cx="8" cy="9" rx="6" ry="4" stroke="currentColor" stroke-width="1.4" />
            <path d="M2 9c0-3 2.7-5.5 6-5.5S14 6 14 9" stroke="currentColor" stroke-width="1.4" />
            <path d="M5 13h6" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" />
        </svg>
        """);

    public static RenderFragment WashingMachine => Svg("""
        <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
            <rect x="1.5" y="1.5" width="13" height="13" rx="2" stroke="currentColor" stroke-width="1.4" />
            <circle cx="8" cy="9" r="3" stroke="currentColor" stroke-width="1.4" />
            <circle cx="5" cy="4" r="0.7" fill="currentColor" />
            <circle cx="7.5" cy="4" r="0.7" fill="currentColor" />
        </svg>
        """);

    public static RenderFragment Play => Svg("""
        <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
            <path d="M4 2.5l8 4.5-8 4.5V2.5z" fill="currentColor" />
        </svg>
        """);

    public static RenderFragment Check => Svg("""
        <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
            <path d="M2 7l4 4 6-6" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
        """);

    public static RenderFragment Wifi => Svg("""
        <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
            <path d="M1 5c3.3-3.3 8.7-3.3 12 0" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" />
            <path d="M3 7.5c2.2-2.2 5.8-2.2 8 0" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" />
            <path d="M5 10c1.1-1.1 2.9-1.1 4 0" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" />
            <circle cx="7" cy="12" r="0.8" fill="currentColor" />
        </svg>
        """);

    public static RenderFragment WifiOff => Svg("""
        <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
            <path d="M2 2l10 10" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" />
            <circle cx="7" cy="12" r="0.8" fill="currentColor" />
        </svg>
        """);

    public static RenderFragment Cpu => Svg("""
        <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
            <rect x="3" y="3" width="8" height="8" rx="1" stroke="currentColor" stroke-width="1.3" />
            <path d="M5 1v2M7 1v2M9 1v2M5 11v2M7 11v2M9 11v2M1 5h2M1 7h2M1 9h2M11 5h2M11 7h2M11 9h2" stroke="currentColor" stroke-width="1.2" stroke-linecap="round" />
        </svg>
        """);

    public static RenderFragment Calendar => Svg("""
        <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
            <rect x="1" y="2.5" width="12" height="10" rx="1.5" stroke="currentColor" stroke-width="1.3" />
            <path d="M1 6h12" stroke="currentColor" stroke-width="1.2" />
            <path d="M4 1v3M10 1v3" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" />
        </svg>
        """);

    public static RenderFragment Plus => Svg("""
        <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
            <path d="M7 2v10M2 7h10" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" />
        </svg>
        """);

    public static RenderFragment Export => Svg("""
        <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
            <path d="M7 2v7M4 6l3-4 3 4" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round" />
            <path d="M2 10v2h10v-2" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" />
        </svg>
        """);

    public static RenderFragment Bolt => Svg("""
        <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
            <path d="M8 1L2 8h5l-1 5 6-7H7l1-5z" fill="currentColor" />
        </svg>
        """);

    private static RenderFragment Svg(string markup) => builder =>
        builder.AddMarkupContent(0, markup);
}
