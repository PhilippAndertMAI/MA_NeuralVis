// Function to convert RGB to HSV
vec3 rgbToHsv(vec3 color) {
    vec3 hsv;
    
    float maxVal = max(max(color.r, color.g), color.b);
    float minVal = min(min(color.r, color.g), color.b);
    
    hsv.z = (maxVal + minVal) / 2.0;
    
    float d = maxVal - minVal;
    
    if (d < 0.00001) {
        hsv.x = 0.0;
        hsv.y = 0.0; // grayscale
    } else {
        hsv.y = (hsv.z < 0.5) ? (d / (maxVal + minVal)) : (d / (2.0 - maxVal - minVal));
        
        if (maxVal == color.r) {
            hsv.x = (color.g - color.b) / d + ((color.g < color.b) ? 6.0 : 0.0);
        } else if (maxVal == color.g) {
            hsv.x = (color.b - color.r) / d + 2.0;
        } else {
            hsv.x = (color.r - color.g) / d + 4.0;
        }
        
        hsv.x /= 6.0;
    }
    
    return hsv;
}

// Helper function for hsvToRgb
float _hueToRgb(float p, float q, float t) {
    if (t < 0.0) t += 1.0;
    if (t > 1.0) t -= 1.0;
    if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
    if (t < 1.0 / 2.0) return q;
    if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
    return p;
}

// Function to convert HSV to RGB
vec3 hsvToRgb(vec3 hsv) {
    vec3 rgb;
    
    if (hsv.y < 0.00001) {
        rgb = vec3(hsv.z);
    } else {
        float q = (hsv.z < 0.5) ? (hsv.z * (1.0 + hsv.y)) : (hsv.z + hsv.y - hsv.z * hsv.y);
        float p = 2.0 * hsv.z - q;
        
        rgb.r = _hueToRgb(p, q, hsv.x + 1.0 / 3.0);
        rgb.g = _hueToRgb(p, q, hsv.x);
        rgb.b = _hueToRgb(p, q, hsv.x - 1.0 / 3.0);
    }
    
    return rgb;
}