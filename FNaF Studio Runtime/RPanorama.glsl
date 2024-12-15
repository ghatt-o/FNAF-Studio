#version 330

uniform sampler2D bg;       // Background texture
uniform float fPixelHeight; // Pixel height scaling factor
uniform bool draw;          // Whether to draw or skip
uniform int zoom;           // Zoom level
uniform int noWrap;         // Wrapping behavior

in vec2 fragTexCoord;       // Fragment texture coordinate (from vertex shader)
out vec4 finalColor;        // Final fragment color

void main()
{
    float fB = 1.0 - (zoom * fPixelHeight);
    float fC = max(0.02, 1.0 + (fB - 1.0) * 4.0 * (fragTexCoord.x - 0.5) * (fragTexCoord.x - 0.5));
    vec2 posTex = fragTexCoord * vec2(1.0, fC) + vec2(0.0, (1.0 - fC) * 0.5);

    float inBounds = step(0.0, posTex.y) * step(posTex.y, 1.0);
    vec4 color = texture(bg, posTex);
    finalColor = mix(vec4(0.0, 0.0, 0.0, 1.0), color, inBounds);
}
