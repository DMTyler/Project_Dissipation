#ifndef DG_MATH
#define  DG_MATH

#ifndef PI
#define PI 3.14159265358979323846
#endif

#ifndef INV_PI
#define INV_PI 0.31830988618379067154
#endif

/// \brief The reflection under tangent space
inline float3 Reflect(float3 wo)
{
    return float3(wo.x, wo.y, -wo.z);
}

/// \param w The tagent space(z-up) vector 
inline float AbsCosTheta(float3 w)
{
    return abs(w.z);
}

/// \brief The cosine of the angle between the normal and the vector
/// \param w The tangent space(z-up) vector
inline float CosTheta(float3 w)
{
    return w.z;
}

/// \brief The cosine square of the angle between the normal and the vector
/// \param w The tangent space(z-up) vector
inline float Cos2Theta(float3 w)
{
    return w.z * w.z;
}

/// \brief The sine square of the angle between the normal and the vector
/// \param w The tangent space(z-up) vector
inline float Sin2Theta(float3 w)
{
    return max(0, 1.0 - Cos2Theta(w));
}

/// \brief The sine of the angle between the normal and the vector
/// \param w The tangent space(z-up) vector
inline float SinTheta(float3 w)
{
    return sqrt(Sin2Theta(w));
}

/// \brief The tangent of the angle between the normal and the vector
/// \param w The tangent space(z-up) vector
inline float TanTheta(float3 w)
{
    return SinTheta(w) / CosTheta(w);
}

/// \brief The tangent square of the angle between the normal and the vector
/// \param w The tangent space(z-up) vector
inline float Tan2Theta(float3 w)
{
    return Sin2Theta(w) / Cos2Theta(w);
}

/// \brief The cosine of the azimuthal angle of the vector 切线空间（Tangent Space）中的方位角（Azimuth Angle）的 cosine 值
/// \param w The tangent space(z-up) vector
inline float CosPhi(float3 w)
{
    float sinTheta = SinTheta(w);
    return sinTheta == 0 ? 1 : clamp(w.x / sinTheta, -1, 1);
}

/// \brief The sine of the azimuthal angle of the vector 切线空间（Tangent Space）中的方位角（Azimuth Angle）的 sine 值
/// \param w The tangent space(z-up) vector
inline float SinPhi(float3 w)
{
    float sinTheta = SinTheta(w);
    return sinTheta == 0 ? 0 : clamp(w.y / sinTheta, -1, 1);
}

/// \brief The cosine square of the azimuthal angle of the vector 切线空间（Tangent Space）中的方位角（Azimuth Angle）的 cosine 平方值
/// \param w The tangent space(z-up) vector
inline float Cos2Phi(float3 w)
{
    const float cosPhi = CosPhi(w);
    return cosPhi * cosPhi;
}

/// \brief The sine square of the azimuthal angle of the vector 切线空间（Tangent Space）中的方位角（Azimuth Angle）的 sine 平方值
/// \param w The tangent space(z-up) vector
inline float Sin2Phi(float3 w)
{
    const float sinPhi = SinPhi(w);
    return sinPhi * sinPhi;
}

/// \brief Whether two vectors are in the same hemisphere
/// \param w The tangent space(z-up) vector
/// \param wp The tangent space(z-up) vector
inline bool SameHemisphere(float3 w, float3 wp)
{
    return w.z * wp.z > 0;
}

/// \brief Geometric correction of the normal distribution (to get the pdf)
/// \param D The normal distribution value
/// \param wh The half vector in tangent space
inline float Pdf_Wh(float D, float3 wh)
{
    return D * AbsCosTheta(wh);
}

/// \brief Set the direction of w into the same hemishpere of v
/// \param w The tangent space(z-up) vector to be set
/// \param v The target direction
inline float3 Faceforward(float3 w, float3 v)
{
    return (dot(w, v) < 0.0f) ? -w : w;
}

/// \brief Transform a direction from world space into tangent space
inline float3 TransformWorldToTangent(float3 worldDir, float3 normalWS, float3 tangentWS, float3 bitangentWS)
{
    return float3(
        dot(worldDir, tangentWS),
        dot(worldDir, bitangentWS),
        dot(worldDir, normalWS)
    );
}

/// \brief Transform a direction from object space into tangent space
inline float3 TransformObjectToTangent(float3 objectDir, float3 normalOS, float3 tangentOS, float3 bitangentOS)
{
    return mul(objectDir, float3x3(tangentOS, bitangentOS, normalOS));
}

/// \brief Transform a direction from tangent space into world space
inline float3 TransformTangentToWorld(float3 tangentDir, float3 normalWS, float3 tangentWS, float3 bitangentWS)
{
    return tangentDir.x * tangentWS + tangentDir.y * bitangentWS + tangentDir.z * normalWS;
}

/// \brief Transform a direction from tangent space into object space
inline float3 TransformTangentToObject(float3 tangentDir, float3 normalOS, float3 tangentOS, float3 bitangentOS)
{
    return tangentDir.x * tangentOS + tangentDir.y * bitangentOS + tangentDir.z * normalOS;
}

/// \brief The reflection under tangent space
inline bool Refract(float3 wi, float eta, out float3 wt)
{
    wi = normalize(wi);
    float CosThetaI = CosTheta(wi);
    float sin2ThetaI = max(0, 1 - CosThetaI * CosThetaI);
    float sin2ThetaT = eta * eta * sin2ThetaI;
    if (sin2ThetaT >= 1)
    {
        wt = float3(0, 0, 0);
        return false;
    }
    float cosThetaT = sqrt(max(0, 1 - sin2ThetaT));
    wt = eta * -wi + (eta * CosThetaI - cosThetaT) * float3(0, 0, 1) ; // (eta * CosThetaI - cosThetaT) * float3(0, 0, 1)
    return true;
}

inline float ang2rad(float x)
{
    return x * PI / 180.0f;
}

inline float rad2ang(float x)
{
    return x * 180.0f / PI;
}

inline float magnitude(float3 v)
{
    return sqrt(dot(v, v));
}

inline float magnitude(float2 v)
{
    return sqrt(dot(v, v));
}

inline float magnitude(float4 v)
{
    return sqrt(dot(v, v));
}

float4x4 inverse(float4x4 m)
{
    float n11 = m[0][0], n12 = m[1][0], n13 = m[2][0], n14 = m[3][0];
    float n21 = m[0][1], n22 = m[1][1], n23 = m[2][1], n24 = m[3][1];
    float n31 = m[0][2], n32 = m[1][2], n33 = m[2][2], n34 = m[3][2];
    float n41 = m[0][3], n42 = m[1][3], n43 = m[2][3], n44 = m[3][3];

    float t11 = n23 * n34 * n42 - n24 * n33 * n42 + n24 * n32 * n43 - n22 * n34 * n43 - n23 * n32 * n44 + n22 * n33 * n44;
    float t12 = n14 * n33 * n42 - n13 * n34 * n42 - n14 * n32 * n43 + n12 * n34 * n43 + n13 * n32 * n44 - n12 * n33 * n44;
    float t13 = n13 * n24 * n42 - n14 * n23 * n42 + n14 * n22 * n43 - n12 * n24 * n43 - n13 * n22 * n44 + n12 * n23 * n44;
    float t14 = n14 * n23 * n32 - n13 * n24 * n32 - n14 * n22 * n33 + n12 * n24 * n33 + n13 * n22 * n34 - n12 * n23 * n34;

    float det = n11 * t11 + n21 * t12 + n31 * t13 + n41 * t14;
    if (det == 0.0) return float4x4(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0); // 如果行列式为 0，返回零矩阵

    float invDet = 1.0 / det;

    float4x4 result;
    result[0][0] = t11 * invDet;
    result[1][0] = (n24 * n33 * n41 - n23 * n34 * n41 - n24 * n31 * n43 + n21 * n34 * n43 + n23 * n31 * n44 - n21 * n33 * n44) * invDet;
    result[2][0] = (n22 * n34 * n41 - n24 * n32 * n41 + n24 * n31 * n42 - n21 * n34 * n42 - n22 * n31 * n44 + n21 * n32 * n44) * invDet;
    result[3][0] = (n23 * n32 * n41 - n22 * n33 * n41 - n23 * n31 * n42 + n21 * n33 * n42 + n22 * n31 * n43 - n21 * n32 * n43) * invDet;

    result[0][1] = t12 * invDet;
    result[1][1] = (n13 * n34 * n41 - n14 * n33 * n41 + n14 * n31 * n43 - n11 * n34 * n43 - n13 * n31 * n44 + n11 * n33 * n44) * invDet;
    result[2][1] = (n14 * n32 * n41 - n12 * n34 * n41 - n14 * n31 * n42 + n11 * n34 * n42 + n12 * n31 * n44 - n11 * n32 * n44) * invDet;
    result[3][1] = (n12 * n33 * n41 - n13 * n32 * n41 + n13 * n31 * n42 - n11 * n33 * n42 - n12 * n31 * n43 + n11 * n32 * n43) * invDet;

    result[0][2] = t13 * invDet;
    result[1][2] = (n14 * n23 * n41 - n13 * n24 * n41 - n14 * n21 * n43 + n11 * n24 * n43 + n13 * n21 * n44 - n11 * n23 * n44) * invDet;
    result[2][2] = (n12 * n24 * n41 - n14 * n22 * n41 + n14 * n21 * n42 - n11 * n24 * n42 - n12 * n21 * n44 + n11 * n22 * n44) * invDet;
    result[3][2] = (n13 * n22 * n41 - n12 * n23 * n41 - n13 * n21 * n42 + n11 * n23 * n42 + n12 * n21 * n43 - n11 * n22 * n43) * invDet;

    result[0][3] = t14 * invDet;
    result[1][3] = (n13 * n24 * n31 - n14 * n23 * n31 + n14 * n21 * n33 - n11 * n24 * n33 - n13 * n21 * n34 + n11 * n23 * n34) * invDet;
    result[2][3] = (n14 * n22 * n31 - n12 * n24 * n31 - n14 * n21 * n32 + n11 * n24 * n32 + n12 * n21 * n34 - n11 * n22 * n34) * invDet;
    result[3][3] = (n12 * n23 * n31 - n13 * n22 * n31 + n13 * n21 * n32 - n11 * n23 * n32 - n12 * n21 * n33 + n11 * n22 * n33) * invDet;

    return result;
}


#endif