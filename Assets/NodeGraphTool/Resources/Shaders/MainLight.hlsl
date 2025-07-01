

void GetMainLightInfo_float(out float3 LightDirection, out float LightIntensity)
{
    #ifdef  SHADERGRAPH_PREVIEW
    LightDirection=float3(0.5,0.5,0);
    LightIntensity=1.0;
    #else
    LightDirection  = GetMainLight().direction;
    LightIntensity =  length(GetMainLight().color);
    #endif
}
