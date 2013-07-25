//Atom CUBE Libs
//using System.IO;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode()]

public class Atom_CUBE  : MonoBehaviour {

    public static Vector3 RGBMDecode (Color rgbmHDR, float max){
        
        Vector3 hdr;
        //tex2.SetPixels( (Color[]) tex1.GetPixels () );
        //tex2.Apply(false); //No mipmap
        float th = rgbmHDR.a * max;

        hdr.x = (float)rgbmHDR.r * th;
        hdr.y = (float)rgbmHDR.g * th;
        hdr.z = (float)rgbmHDR.b * th;

        return hdr;
    }

    public static float GetExpo (float ev){
        //EV
        float exp = 1;
        if(ev < 0){
            exp = 1/Mathf.Pow(2,-ev);
        }
        
        return exp;
    }

    public static Color ToneMappingHDR (Vector3 hdr, float ev){
        //EV
        hdr *= GetExpo(ev);
        Color col = new Color(hdr.x,hdr.y,hdr.z,1);

        return col;
    }

    public static void FillColor2Tex(Texture2D tex, Color color){
        for (int y = 0; y < tex.height; ++y) {
            for (int x = 0; x < tex.width; ++x) {
                tex.SetPixel (x, y, color);
            }
        }
    }

    public static void FillColor2Tex(Rect rect, Texture2D tex, Color color){
        if((int)rect.width > tex.width || (int)rect.height > tex.height ){
            Debug.LogError("The size of Rect is bigger than texture.");
            return;
        }
        for (int y = (int)rect.y; y < (int)rect.height; ++y) {
            for (int x = (int)rect.x; x < (int)rect.width; ++x) {
                tex.SetPixel (x, y, color);
            }
        }
    }

    public static Vector3 TexRotate180(float u, float v, float width, float height){
        return new Vector3(width - u, height - v);
    }

    public static Vector3 TexRotate90(float u, float v, float width, float height){
        return new Vector3(v, height - u);
    }

    public static Vector3 TexRotate270(float u, float v, float width, float height){
        return new Vector3(width - v, u);
    }

    public static Vector3 TexRotateHorizontal(float u, float v, float width, float height){
        return new Vector3(width - u, v);
    }

    public static Vector3 TexRotateVertical(float u, float v, float width, float height){
        return new Vector3(u, height - v);
    }

    public static void Tex2TexByPixelRGB(Texture2D tex1, Texture2D tex2){
        Color col = new Color(0,0,0,1);

        //Atom_Texture.Point (tex1, tex2.width, tex2.height);
        for (int y = 0; y < tex1.height; ++y) {
            for (int x = 0; x < tex1.width; ++x) {
                col = tex1.GetPixel(x,y);
                col.a = 1.0f;
                tex2.SetPixel (x, y, col);
            }
        }

        tex2.Apply(false); //No mipmap
    }

    public static void Tex2TexByPixelAlpha(Texture2D tex1, Texture2D tex2){
        Color col = new Color(0,0,0,1);

        //Atom_Texture.Point (tex1, tex2.width, tex2.height);
        for (int y = 0; y < tex1.height; ++y) {
            for (int x = 0; x < tex1.width; ++x) {
                col.r = col.g = col.b = tex1.GetPixel(x,y).a;
                tex2.SetPixel (x, y, col);
            }
        }

        tex2.Apply(false); //No mipmap
    }


    //Copy texture1 to another texture2, it will resize texture to tex
    public static bool Tex2Tex (Texture2D tex1, Texture2D tex2, int channel = 0, bool useBilinear = true, bool mipmap = false ){
        Texture2D dumyTex = (Texture2D)Instantiate (tex1);

        if(dumyTex.width != tex2.width || dumyTex.height != tex2.height){
            Debug.Log("Resize texture and copy it to preview.");

            if(useBilinear) {
                Atom_Texture.Bilinear (dumyTex, tex2.width, tex2.height);
            }
            else {
                Atom_Texture.Point (dumyTex, tex2.width, tex2.height);
            }
            
            
        }
    
        Debug.Log("Copy texture to preview.");

        switch (channel){
            case 0: //Just regular copy
                tex2.SetPixels( (Color[]) dumyTex.GetPixels () );
                tex2.Apply(mipmap); //No mipmap
            break;

            case 1: //Only RGB Channel
                Tex2TexByPixelRGB(dumyTex, tex2);
            break;

            case 2: //Only Alpha Channel
                Tex2TexByPixelAlpha(dumyTex, tex2);
            break;

            default: //Do nothing
            break;
        }
        

        return true;
    }


    //Resample cube by different size
    public static bool CUBE2CUBE (Cubemap cube1, Cubemap cube2, bool useBilinear = true, bool mipmap = false ){

        if(cube1.width == cube2.width) {

            for(int n = 0; n < 6; n++){
                cube2.SetPixels(cube1.GetPixels((CubemapFace)n), (CubemapFace)n);
            }
            cube2.Apply(mipmap);
            return true;
            
        }

        //bool hasMipMap = cube1.mipmapCount() > 0 ? true : false;
        Texture2D faceTex1 = new Texture2D(cube1.width, cube1.height, cube1.format, mipmap);
        Texture2D faceTex2 = (Texture2D)Instantiate (faceTex1);
        //Texture2D faceTex2 = new Texture2D(cube2.width, cube2.height, cube1.format, hasMipMap);

        for(int n = 0; n < 6; n++){

            SetCUBEFace2Tex(cube1, (CubemapFace)n, faceTex1, mipmap);

            if(useBilinear) {
                Atom_Texture.Bilinear (faceTex2, cube2.width, cube2.height);
            }
            else {
                Atom_Texture.Point (faceTex2, cube2.width, cube2.height);
            }

            SetTex2CUBEFace(faceTex2, cube2, (CubemapFace)n, mipmap);

        }
        

        return true;
    }

    


    public static bool RGBMHDR2Tex (Texture2D texRGBM, Texture2D tex, float maxRange , float exposureValue, bool mipmap = false ){
        if(texRGBM.width != tex.width || texRGBM.height != tex.height ){
            //Texture2D dumyTex = (Texture2D)Instantiate (texRGBM);
            Atom_Texture.Point (texRGBM, tex.width, tex.height);
        }

        for (int y = 0; y <= texRGBM.height; ++y) {
            for (int x = 0; x <= texRGBM.width; ++x) {
                tex.SetPixel (x, y, ToneMappingHDR(RGBMDecode(texRGBM.GetPixel(x,y), maxRange), exposureValue));
            }
        }
        
        tex.Apply(mipmap); //No mipmap

        return true;
    }


    public static void SetCUBEPixel (Cubemap cube, CubemapFace face, int x, int y, Color col, bool sky2cube = false){
        if(sky2cube){
            //Invert X Y Axis
            cube.SetPixel (face, (cube.width-1) - x, (cube.height-1) - y, col);            
        }
        else {
            //Invert Y Axis
            cube.SetPixel (face, x, (cube.height-1) - y, col);   

            //cube.SetPixels (col,face);
        }

        cube.Apply();
    }


    public static void SetCUBEPixels (Cubemap cube, CubemapFace face, Color[] col, bool sky2cube = false){
        if(sky2cube){
            //Invert X Y Axis
            for (int y = 0; y < cube.height; ++y) {
                for (int x = 0; x < cube.width; ++x) {
                    cube.SetPixel (face, (cube.width-1) - x, (cube.height-1) - y, col[x+y*cube.width]);
                }
            }
            
        }
        else {
            //Invert Y Axis

            for (int y = 0; y < cube.height; ++y) {
                for (int x = 0; x < cube.width; ++x) {
                    cube.SetPixel (face, x, (cube.height-1) - y, col[x+y*cube.width]);
                }
            }
            //

            //cube.SetPixels (col,face);
        }

        cube.Apply();
    }



    public static Color GetCUBEPixel (Cubemap cube, CubemapFace face, int x, int y,bool sky2cube = false){
        Color col; 
        if(sky2cube){
            //Invert X Y Axis
            col = cube.GetPixel(face, (cube.width-1) - x, (cube.height-1) - y);
        }
        else {
            //Invert Y Axis
            col = cube.GetPixel(face, x, (cube.height-1) - y);
            //
        }
        return col;
    }



    public static Color[] GetCUBEPixels (Cubemap cube, CubemapFace face, bool sky2cube = false){
        Color[] col = new Color[cube.width * cube.height]; 
        if(sky2cube){
            //Invert X Y Axis
            for (int y = 0; y < cube.height; ++y) {
                for (int x = 0; x < cube.width; ++x) {
                    col[x+y*cube.width] = cube.GetPixel(face, (cube.width-1) - x, (cube.height-1) - y);
                }
            }
        }
        else {
            //Invert Y Axis
            for (int y = 0; y < cube.height; ++y) {
                for (int x = 0; x < cube.width; ++x) {
                    col[x+y*cube.width] = cube.GetPixel(face, x, (cube.height-1) - y);
                }
            }
            //
        }
        return col;
    }

    public static Vector2 GetUV(int x, int y, int width, int height ){
        return new Vector2( (float)((float)(x+1)/(float)width) , (float)((float)(y+1)/(float)height) );
    }

    public static Vector3 GetFilterCUBEVec(Vector2 UV, int face){

        Vector3 VEC;
        UV = UV * 2.0f - new Vector2(1.0f,1.0f); // Range to -1 to 1


        switch( face ) {
            case 0 : //PositiveX   Right facing side (+x).
                VEC = new Vector3(1.0f,UV.y,UV.x);
            break;

            case 1 : //NegativeX   Left facing side (-x).
                VEC = new Vector3(-1.0f,UV.y,-UV.x);
            break;

            case 2 : //PositiveY   Upwards facing side (+y).
                VEC = new Vector3(-UV.x,-1.0f,UV.y);
            break;

            case 3 : //NegativeY   Downward facing side (-y).
                VEC = new Vector3(-UV.x,1.0f,-UV.y);
            break;

            case 4 : //PositiveZ   Forward facing side (+z).
                VEC = new Vector3(-UV.x,UV.y,1.0f);
            break;

            case 5 : //NegativeZ   Backward facing side (-z).
                VEC = new Vector3(UV.x,UV.y,-1.0f);
            break;

            default : 
                VEC = new Vector3( 0.0f, 0.0f, 1.0f );
            break;
        }   

        return VEC.normalized;

    }
    public static Color GetCUBEConvolution (Cubemap cube, Vector3 dir, float range, float power = 1.0f) {

        Color col = new Color(0,0,0,1);
        Vector4 result = new Vector4(0,0,0,1);
  
        //range = 1- range; // from 0-1 to 1-0 (cos)
        //if(range <= 0.001f){
            //stepPixel = 1;

        //    col = cube.GetPixel((CubemapFace)info.z, (int)info.x, (int)info.y);
        //}
        //else{
            float sum = 0.0f;
            for(int N = 0; N < 6; N++){
                for(int Y = 0; Y < cube.height; Y++){
                    for(int X = 0; X < cube.width; X++){

                        Vector3 vecSampler = GetFilterCUBEVec(GetUV((cube.width-1) - X, Y, cube.width, cube.height), N);
                        
                        if(range <= 0.01f || power <= 1.01f) { //Diffuse
                            float weight = Mathf.Max(0,Vector3.Dot(vecSampler, dir));
                            if(weight >= range){
                                col = weight * cube.GetPixel((CubemapFace)N, X, Y);
                                result.x += col.r;
                                result.y += col.g;
                                result.z += col.b;
                                result.w += col.a;
                                //sum++;
                                sum += weight;
                            }
                        }
                        else { //Blur Specular
                            float weight = Mathf.Max(0,Vector3.Dot(vecSampler, dir));
                            weight = Mathf.Pow(weight,power);
                            if(weight >= range){
                                col = weight * cube.GetPixel((CubemapFace)N, X, Y);
                                result.x += col.r;
                                result.y += col.g;
                                result.z += col.b;
                                result.w += col.a;
                                //sum++;
                                sum += weight;
                            }

                        }
                        
                        
                        //col = new Color(1,0,0,1);
                    }
                }
                
                    
            }
            col = (Color)result/sum;
        //}

        return col;
    }

    public static bool FilterCUBE_Diffuse (Cubemap cube, Cubemap filterCube, int size, float range, float power) {

        //float U,V = 0;
        if (cube.width == filterCube.width && cube.height == filterCube.height && cube.format == filterCube.format) {

            if(range <= 0.001f) {
                for(int n = 0; n < 6; n++){
                    filterCube.SetPixels(cube.GetPixels((CubemapFace)n), (CubemapFace)n);
                }
                filterCube.Apply();
            }
            else {

                Cubemap tmpCube = new Cubemap (size , cube.format, false);
                Cubemap tmpFilterCube = new Cubemap (size , filterCube.format, false);
                CUBE2CUBE (cube, tmpCube);


                Color colFilter = new Color(0,0,0,1);
                for(int n = 0; n < 6; n++){
                    for (int y=0; y < size; y++) {
                        for(int x=0; x < size; x++) {
                            
                            colFilter = GetCUBEConvolution(tmpCube, GetFilterCUBEVec(GetUV((size-1) - x, y, size, size), n), range, power);

                            tmpFilterCube.SetPixel((CubemapFace)n, x, y, colFilter);
                        }
                    }
                }
                tmpFilterCube.Apply();
                CUBE2CUBE (tmpFilterCube, filterCube);
            }
            
            
            return true;
        }
        else {
            return false;
        }

    }


    public static bool RT2TEX (RenderTexture rt, Texture2D tex, bool linear, bool mipmap = false) {


        // Read screen contents into the texture
        RenderTexture.active = rt;
        tex.ReadPixels (new Rect(0, 0, rt.width, rt.height), 0, 0,mipmap); // render texture ???
        tex.Apply(mipmap);

        RenderTexture.active = null; // added to avoid errors 

        return true;
    }


    public static bool RT2CUBE (RenderTexture rt, Cubemap cube, CubemapFace face, bool hasAlpha, bool linear, bool sky2cube = false, bool mipmap = false ) {

        //mipmap = mipmap?? false;

        //cube = null;
        if(rt.width != rt.height){
            Debug.LogError("The texture's width is not same as height!");
            return false;
        }

        TextureFormat texFormat;
        if(hasAlpha){
            texFormat = TextureFormat.ARGB32;
        }
        else{
            texFormat = TextureFormat.RGB24;
        }

        //cube = new Cubemap (rt.width, texFormat, mipmap);
        Texture2D tex = new Texture2D(rt.width, rt.height, texFormat, mipmap, linear);

        bool status = RT2TEX(rt, tex, linear, mipmap);
        if (!status){
            Debug.LogError("The Function: RenderTexture to Texture2D Error!");
            return false;
        }


        SetCUBEPixels( cube, face, (Color[]) tex.GetPixels() , sky2cube);

        DestroyImmediate(tex);

        return true;
    }

    public static bool SetCUBEFace2Tex (Cubemap cube, CubemapFace face, Texture2D tex, bool mipmap = false){
        if(cube.width != cube.height){
            Debug.LogError("The cubemap size error!");
            return false;
        }

        if(tex.width == cube.width && tex.height  == cube.height){ // Size check

            tex.SetPixels ((Color[]) cube.GetPixels(face) ); 

            tex.Apply(mipmap);
        }
        else{
            Debug.LogError("The texture size is not as same as cube face size!");
            return false;
        }
        
        return true;
    }


    public static bool SetTex2CUBEFace (Texture2D tex, Cubemap cube, CubemapFace face, bool mipmap = false){
        if(cube.width != cube.height){
            Debug.LogError("The cubemap size error!");
            return false;
        }

        if(tex.width == cube.width && tex.height  == cube.height){ // Size check

            cube.SetPixels ((Color[]) tex.GetPixels(), face ); 

            cube.Apply(mipmap);
        }
        else{
            Debug.LogError("The texture size is not as same as cube face size!");
            return false;
        }
        
        return true;
    }


    public static bool CUBEFace2Tex (Texture2D tex, Cubemap cube, int face, bool sky2cube = false, bool mipmap = false){
        if(cube.width != cube.height){
            Debug.LogError("The cubemap size error!");
            return false;
        }

        if(tex.width == cube.width && tex.height  == cube.height){ // Size check

            tex.SetPixels ((Color[]) GetCUBEPixels ( cube, (CubemapFace)face , sky2cube) ); 

            tex.Apply(mipmap);
        }
        else{
            Debug.LogError("The texture size is not as same as cube face size!");
            return false;
        }
        
        return true;
    }


    public static bool CUBEFace2Tex (Texture2D tex, Rect area, Cubemap cube, int face, bool sky2cube = false, int rotation = 0, bool mipmap = false){
        if(cube.width != cube.height){
            Debug.LogError("The cubemap size error!");
            return false;
        }
        //function SetPixels (x : int, y : int, blockWidth : int, blockHeight : int, colors : Color[], miplevel : int = 0) 
        if((int)area.width == cube.width && (int)area.height  == cube.height){ // Size check
            if(rotation == 0) {
                //tex.SetPixels ((int)area.x, (int)area.y, (int)area.width, (int)area.height, (Color[]) cube.GetPixels ( (CubemapFace)face ));
                tex.SetPixels ((int)area.x, (int)area.y, (int)area.width, (int)area.height, (Color[]) GetCUBEPixels ( cube, (CubemapFace)face , sky2cube) );
                tex.Apply(mipmap);
            }
            //90
            else if(rotation == 1) { 

            }
            //180
            else if(rotation == 2) { 
                Texture2D tmpTex = new Texture2D(cube.width, cube.height, cube.format, mipmap);
                Texture2D tmpTex2 = new Texture2D(cube.width, cube.height, cube.format, mipmap);
                CUBEFace2Tex(tmpTex, cube, face, sky2cube);

                for (int y = 0; y < tmpTex.height; ++y) {
                    for (int x = 0; x < tmpTex.width; ++x) {
                        Vector3 v = TexRotate180(x, y, tmpTex.width, tmpTex.height);
                        tmpTex2.SetPixel (x, y, tmpTex.GetPixel((int)v.x, (int)v.y));
                    }
                }
                tmpTex2.Apply(mipmap);

                tex.SetPixels ((int)area.x, (int)area.y, (int)area.width, (int)area.height, (Color[]) tmpTex2.GetPixels ()); 
                tex.Apply(mipmap);
            }


            //270 or -90
            else if(rotation == 3) { 

            }

            else {
                Debug.LogError("The rotation mode is not exist!");
                return false;
            }
            

        }
        else{
            Debug.LogError("The Rect area size is not as same as cube face size!");
            return false;
        }
        
        return true;
    }


/*
public static void test (Texture2D tex){
    Color col = new Color(0,0,1,1);
    for (int y = 0; y < tex.height; ++y) {
        for (int x = 0; x < tex.width; ++x) {
            int n = x&y;
            //bool b = n>(tex.width * tex.height)/2;
            bool b = n>0;
            col = b ? Color.white : Color.gray;
            tex.SetPixel (x, y, col);
        }
    }
           
    tex.Apply(false);
}
*/

    public static bool CUBE2Tex (Cubemap cube, Texture2D tex, int mode, bool linear, int channel = 0, bool sky2cube = false, bool mipmap = false) {

        if(tex.format == cube.format || tex.format == TextureFormat.RGB24)
        {
            int rX = 0;
            int rY = 0;
            Material mat = null;
            RenderTexture rt = null;
            Texture2D texTmp = null;
            Rect rectArea = new Rect(0,0,0,0);
            switch (mode){
                case 0: //Preview cube face 1,4,0,5 in one texture 5,0,4,1

                    if(tex.width/tex.height != 4){
                        Debug.LogError("The size ratio of the texture is incorrect! Case 0 should 4:1, but it is: " + tex.width/tex.height);
                        return false;
                    }

                    texTmp = new Texture2D(cube.width * 4,cube.height,cube.format, false);
                    Debug.Log("Cube size: " + cube.width + " x " + cube.height);

                    rectArea.width = (float)cube.width;
                    rectArea.height = (float)cube.height;
                    Debug.Log("Rect Size: " + rectArea);
                    //rectArea = new Rect( 0, 0, cube.width, cube.height );

                    if(!sky2cube){
                        CUBEFace2Tex(texTmp, rectArea, cube, 1, sky2cube);//-X, Left

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 4, sky2cube);//+Z, Front

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 0, sky2cube);//+X, Right

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 5, sky2cube);//-Z, Back
                    }
                    else{
                        CUBEFace2Tex(texTmp, rectArea, cube, 0, sky2cube);//+X, Right

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 4, sky2cube);//+Z, Front

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 1, sky2cube);//-X, Left

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 5, sky2cube);//-Z, Back
                    }
                    
                    
                    Tex2Tex(texTmp, tex, channel, false);

                break;

                case 1: //h:w = 4:3 Horizontal Cross -|--

                    if((float)tex.width/tex.height < 1.33f && (float)tex.width/tex.height > 1.34f){
                        Debug.LogError("The size ratio of the texture is incorrect! Case 1 should 4:3, but it is: " + tex.width/tex.height);
                        return false;
                    }


                    texTmp = new Texture2D(cube.width * 4,cube.height * 3,cube.format, false);
                    Debug.Log("Cube size: " + cube.width + " x " + cube.height);

                    //Black background

                    FillColor2Tex(texTmp, new Color(0,0,0,1));

                    rectArea.width = (float)cube.width;
                    rectArea.height = (float)cube.height;
                    Debug.Log("Rect Size: " + rectArea);

                    if(!sky2cube){
                        rectArea.y += (float)cube.height;
                        CUBEFace2Tex(texTmp, rectArea, cube, 1, sky2cube);//-X, Left

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 4, sky2cube);//+Z, Front

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 0, sky2cube);//+X, Right

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 5, sky2cube);//-Z, Back

                        rectArea.x = (float)cube.width;
                        rectArea.y = (float)cube.height * 2;
                        CUBEFace2Tex(texTmp, rectArea, cube, 2, sky2cube);//+Y, Top

                        rectArea.x = (float)cube.width;
                        rectArea.y = 0;
                        CUBEFace2Tex(texTmp, rectArea, cube, 3, sky2cube);//-Y, Bottom
                    }
                    else{ // cube invert X
                        rectArea.y += (float)cube.height;
                        CUBEFace2Tex(texTmp, rectArea, cube, 0, sky2cube);//+X, Right

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 4, sky2cube);//+Z, Front

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 1, sky2cube);//-X, Left

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 5, sky2cube);//-Z, Back

                        rectArea.x = (float)cube.width;
                        rectArea.y = (float)cube.height * 2;
                        CUBEFace2Tex(texTmp, rectArea, cube, 2, sky2cube);//+Y, Top

                        rectArea.x = (float)cube.width;
                        rectArea.y = 0;
                        CUBEFace2Tex(texTmp, rectArea, cube, 3, sky2cube);//-Y, Bottom
                    }
                    

                    Tex2Tex(texTmp, tex, channel, false);

                break;

                case 2: //h:w = 3:4 Vertical Cross

                    if((float)tex.width/tex.height != 0.75f){
                        Debug.LogError("The size ratio of the texture is incorrect! Case 2 should 3:4, but it is: " + (float)tex.width/tex.height);
                        return false;
                    }


                    texTmp = new Texture2D(cube.width * 3,cube.height * 4,cube.format, false);
                    Debug.Log("Cube size: " + cube.width + " x " + cube.height);

                    //Black background

                    FillColor2Tex(texTmp, new Color(0,0,0,1));

                    rectArea.width = (float)cube.width;
                    rectArea.height = (float)cube.height;
                    Debug.Log("Rect Size: " + rectArea);

                    if(!sky2cube){
                        rectArea.y += (float)cube.height * 2;
                        CUBEFace2Tex(texTmp, rectArea, cube, 1, sky2cube);//-X, Left

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 4, sky2cube);//+Z, Front

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 0, sky2cube);//+X, Right

                        rectArea.x = (float)cube.width;
                        rectArea.y = (float)cube.height * 3;
                        CUBEFace2Tex(texTmp, rectArea, cube, 2, sky2cube);//+Y, Top

                        rectArea.x = (float)cube.width;
                        rectArea.y = (float)cube.height;
                        CUBEFace2Tex(texTmp, rectArea, cube, 3, sky2cube);//-Y, Bottom

                        //rectArea.x += (float)cube.width;
                        rectArea.y = 0;
                        CUBEFace2Tex(texTmp, rectArea, cube, 5, sky2cube, 2);//-Z, Back Rotate 180
                    }
                    else{
                        rectArea.y += (float)cube.height * 2;
                        CUBEFace2Tex(texTmp, rectArea, cube, 0, sky2cube);//+X, Right

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 4, sky2cube);//+Z, Front

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 1, sky2cube);//-X, Left

                        rectArea.x = (float)cube.width;
                        rectArea.y = (float)cube.height * 3;
                        CUBEFace2Tex(texTmp, rectArea, cube, 2, sky2cube);//+Y, Top

                        rectArea.x = (float)cube.width;
                        rectArea.y = (float)cube.height;
                        CUBEFace2Tex(texTmp, rectArea, cube, 3, sky2cube);//-Y, Bottom

                        //rectArea.x += (float)cube.width;
                        rectArea.y = 0;
                        CUBEFace2Tex(texTmp, rectArea, cube, 5, sky2cube, 2);//-Z, Back Rotate 180
                    }
                    

                    Tex2Tex(texTmp, tex, channel, false);

                break;

                case 3: //h:w = 6:1 (NVidia) right|left|top|bottom|front|back//Preview cube map as a sky map
                    if(tex.width/tex.height != 6){
                        Debug.LogError("The size ratio of the texture is incorrect! Case 3 should 6:1, but it is: " + tex.width/tex.height);
                        return false;
                    }


                    texTmp = new Texture2D(cube.width * 6,cube.height,cube.format, false);
                    Debug.Log("Cube size: " + cube.width + " x " + cube.height);


                    rectArea.width = (float)cube.width;
                    rectArea.height = (float)cube.height;
                    Debug.Log("Rect Size: " + rectArea);

                    if(!sky2cube){
                        CUBEFace2Tex(texTmp, rectArea, cube, 0, sky2cube);//+X, Right

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 1, sky2cube);//-X, Left

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 2, sky2cube);//+Y, Top

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 3, sky2cube);//-Y, Bottom
                        
                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 4, sky2cube);//+Z, Front

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 5, sky2cube);//-Z, Back 
                    }
                    else{
                        CUBEFace2Tex(texTmp, rectArea, cube, 1, sky2cube);//-X, Left

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 0, sky2cube);//+X, Right

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 2, sky2cube);//+Y, Top

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 3, sky2cube);//-Y, Bottom
                        
                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 4, sky2cube);//+Z, Front

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 5, sky2cube);//-Z, Back 
                    }

                    
                    

                    Tex2Tex(texTmp, tex, channel, false);

                break;

                case 4: //h:w = 6:1 (XSI strip) right->|left<-|back|front<>|bottom|top%
                    if(tex.width/tex.height != 6){
                        Debug.LogError("The size ratio of the texture is incorrect! Case 4 should 6:1, but it is: " + tex.width/tex.height);
                        return false;
                    }


                    texTmp = new Texture2D(cube.width * 6,cube.height,cube.format, false);
                    Debug.Log("Cube size: " + cube.width + " x " + cube.height);


                    rectArea.width = (float)cube.width;
                    rectArea.height = (float)cube.height;
                    Debug.Log("Rect Size: " + rectArea);

                    if(!sky2cube){
                        CUBEFace2Tex(texTmp, rectArea, cube, 0, sky2cube);//+X, Right

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 1, sky2cube);//-X, Left

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 5, sky2cube);//-Z, Back

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 4, sky2cube);//+Z, Front

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 3, sky2cube);//-Y, Bottom

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 2, sky2cube);//+Y, Top
                    }
                    else{
                        CUBEFace2Tex(texTmp, rectArea, cube, 1, sky2cube);//-X, Left

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 0, sky2cube);//+X, Right

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 5, sky2cube);//-Z, Back

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 4, sky2cube);//+Z, Front

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 3, sky2cube);//-Y, Bottom

                        rectArea.x += (float)cube.width;
                        CUBEFace2Tex(texTmp, rectArea, cube, 2, sky2cube);//+Y, Top
                    }
                    

                    Tex2Tex(texTmp, tex, channel, false);

                break;

                case 5: //Spherical map (Latitiude Longitude)
                    rX = 4;
                    rY = 2;
                    if(tex.width/tex.height != rX/rY){
                        Debug.LogError("The size ratio of the texture is incorrect! Case 5 should 2:1, but it is: " + tex.width/tex.height);
                        return false;
                    }


                    texTmp = new Texture2D(cube.width * rX,cube.height * rY,cube.format, false);
                    Debug.Log("Cube size: " + cube.width + " x " + cube.height);

                    //Black background

                    //FillColor2Tex(texTmp, new Color(0,0,0,1));

                    mat = new Material(Shader.Find ("Custom/INT/Atom_Cube2Spherical"));
                    //mat = new Material(Shader.Find ("Custom/Atom_Test"));
                    mat.SetTexture ( "_Cube", cube );

                    if(sky2cube){ //Invert X
                        mat.SetFloat ( "_Mode", 1.0f ); // Sky
                    }
                    else{//
                        mat.SetFloat ( "_Mode", 0.0f ); // Cube
                    }
                    

                    rt = new RenderTexture(cube.width * rX, cube.height * rY, 16, RenderTextureFormat.ARGB32);
                    rt.useMipMap = false;
                    rt.isPowerOfTwo = false;
                    //RGBMface0.isCubemap = true;
                    rt.hideFlags = HideFlags.HideAndDontSave;

                    Graphics.Blit(cube, rt, mat); //Encode RGBM HDR Format

                    RT2TEX(rt, texTmp, linear, mipmap);

                    Tex2Tex(texTmp, tex, channel, false);

                    //rt = null;
                    //mat = null;
                break;

                case 6: //Light Probe map
                    rX = 3;//Atom_SOTools.ratioExportCubeX[mode];
                    rY = 3;//Atom_SOTools.ratioExportCubeY[mode];
                    if(tex.width/tex.height != rX/rY){
                        Debug.LogError("The size ratio of the texture is incorrect! Case 6 should 1:1, but it is: " + tex.width/tex.height);
                        return false;
                    }


                    texTmp = new Texture2D(cube.width * rX,cube.height * rY,cube.format, false);
                    Debug.Log("Cube size: " + cube.width + " x " + cube.height);

                    //Black background

                    //FillColor2Tex(texTmp, new Color(0,0,0,1));

                    mat = new Material(Shader.Find ("Custom/INT/Atom_Cube2LP"));
                    mat.SetTexture ( "_Cube", cube );

                    if(sky2cube){ //Invert X
                        mat.SetFloat ( "_Mode", 1.0f ); // Sky
                    }
                    else{//
                        mat.SetFloat ( "_Mode", 0.0f ); // Cube
                    }

                    rt = new RenderTexture(cube.width * rX, cube.height * rY, 16, RenderTextureFormat.ARGB32);
                    rt.useMipMap = false;
                    rt.isPowerOfTwo = false;
                    //RGBMface0.isCubemap = true;
                    rt.hideFlags = HideFlags.HideAndDontSave;

                    Graphics.Blit(cube, rt, mat); //Encode RGBM HDR Format

                    RT2TEX(rt, texTmp, linear, mipmap);

                    Tex2Tex(texTmp, tex, channel, false);

                break;

                case 7: //

                break;

                case 8: //

                break;

                case 9: //

                break;

                case 10: //

                break;

                case 11: //Preview cube map as a sky map

                break;

                default: //Do nothing

                break;
            }
            DestroyImmediate(rt);
            DestroyImmediate(mat);
            DestroyImmediate(texTmp);

        }

        else{
            Debug.LogError("The texture's format is not as same as Cubemap!");
            return false;
        }

        // Read screen contents into the texture
        
        //DestroyImmediate(rectArea);

        


        return true;
    }


}


//#if UNITY_IPHONE
//#elif UNITY_ANDROID
