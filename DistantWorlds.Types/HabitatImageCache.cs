﻿// Decompiled with JetBrains decompiler
// Type: DistantWorlds.Types.HabitatImageCache
// Assembly: DistantWorlds.Types, Version=1.9.5.12, Culture=neutral, PublicKeyToken=null
// MVID: C87DBA0E-BD3A-46BA-A8F0-EE9F5E5721E2
// Assembly location: H:\7\DistantWorlds.Types.dll

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;

namespace DistantWorlds.Types
{
  public class HabitatImageCache
  {
    private object _LockObject = new object();
    private List<string> _Filepaths = new List<string>();
    private Hashtable _Images = new Hashtable();
    private Hashtable _LastUsage = new Hashtable();
    private Hashtable _ImagesSmall = new Hashtable();
    private int _ImageSmallSize = 30;
    private string _ApplicationStartupPath;
    private string _CustomizationSetName;
    private bool _IsCustomSet;

    public string CustomizationSetName => this._CustomizationSetName;

    public bool IsCustomSet => this._IsCustomSet;

    public void Initialize(
      string applicationStartupPath,
      string customizationSetName,
      bool initialLoad)
    {
      this._ApplicationStartupPath = applicationStartupPath;
      this._CustomizationSetName = customizationSetName;
      bool hasCustom = false;
      this.GenerateHabitatImageFilepaths(initialLoad, out hasCustom);
      if (!initialLoad && !this._IsCustomSet && !hasCustom)
      {
        this._IsCustomSet = hasCustom;
      }
      else
      {
        this.ClearImageCache(this._Images);
        this.ClearImageCache(this._ImagesSmall);
        this.LoadImagesSmall();
      }
    }

    public void ClearOldImages() => this.ClearOldImages(60);

    public void ClearOldImages(int maximumAgeInSeconds)
    {
      DateTime dateTime1 = DateTime.Now.Subtract(new TimeSpan(0, 0, maximumAgeInSeconds));
      List<int> intList = new List<int>();
      lock (this._LockObject)
      {
        foreach (int key in (IEnumerable) this._Images.Keys)
        {
          if (this._LastUsage[(object) key] is DateTime dateTime2 && dateTime2 < dateTime1)
          {
            object image = this._Images[(object) key];
            if (image != null && image is Bitmap)
              ((Image) image).Dispose();
            intList.Add(key);
          }
        }
        for (int index = 0; index < intList.Count; ++index)
        {
          this._Images.Remove((object) intList[index]);
          this._LastUsage.Remove((object) intList[index]);
        }
      }
    }

    public Bitmap[] GetImagesSmall()
    {
      List<Bitmap> bitmapList = new List<Bitmap>();
      for (int key = 0; key < this._Filepaths.Count; ++key)
      {
        object obj = this._ImagesSmall[(object) key];
        if (obj != null && obj is Bitmap)
          bitmapList.Add((Bitmap) obj);
      }
      return bitmapList.ToArray();
    }

    public Bitmap ObtainImageSmall(Habitat habitat)
    {
      Bitmap imageSmall = (Bitmap) null;
      if (habitat != null)
        imageSmall = this.ObtainImageSmall((int) habitat.PictureRef);
      return imageSmall;
    }

    public Bitmap ObtainImageSmall(int pictureRef)
    {
      object obj = this._ImagesSmall[(object) pictureRef];
      return obj == null ? this.CacheImageSmall(pictureRef) : (!(obj is Bitmap) ? this.CacheImageSmall(pictureRef) : (Bitmap) obj);
    }

    public Bitmap ObtainImage(Habitat habitat)
    {
      Bitmap image = (Bitmap) null;
      if (habitat != null)
        image = this.ObtainImage((int) habitat.PictureRef);
      return image;
    }

    public Bitmap ObtainImage(int pictureRef)
    {
      object image1 = this._Images[(object) pictureRef];
      Bitmap image2;
      if (image1 != null)
      {
        if (image1 is Bitmap)
        {
          image2 = (Bitmap) image1;
          if (image2.PixelFormat == PixelFormat.Undefined)
            image2 = this.CacheImage(pictureRef);
          else
            this._LastUsage[(object) pictureRef] = (object) DateTime.Now;
        }
        else
          image2 = this.CacheImage(pictureRef);
      }
      else
        image2 = this.CacheImage(pictureRef);
      return image2;
    }

    public Bitmap FastGetImage(int pictureRef, out bool smallImageSupplied)
    {
      smallImageSupplied = false;
      Bitmap image1 = (Bitmap) null;
      object image2 = this._Images[(object) pictureRef];
      this._LastUsage[(object) pictureRef] = (object) DateTime.Now;
      if (image2 != null)
      {
        image1 = !(image2 is Bitmap) ? this.ObtainImage(pictureRef) : (Bitmap) image2;
      }
      else
      {
        object obj = this._ImagesSmall[(object) pictureRef];
        if (obj != null && obj is Bitmap)
          image1 = (Bitmap) obj;
        smallImageSupplied = true;
        this._Images[(object) pictureRef] = obj;
        this.LoadImageDataDelayed(pictureRef);
      }
      return image1;
    }

    private void LoadImageDataDelayedDelegate(object key)
    {
      if (key == null || !(key is int pictureRef))
        return;
      this.CacheImage(pictureRef);
    }

    public void LoadImageDataDelayed(int pictureRef) => ThreadPool.QueueUserWorkItem(new WaitCallback(this.LoadImageDataDelayedDelegate), (object) pictureRef);

    private Bitmap CacheImageSmall(Habitat habitat)
    {
      Bitmap bitmap = (Bitmap) null;
      if (habitat != null)
        bitmap = this.CacheImageSmall((int) habitat.PictureRef);
      return bitmap;
    }

    private Bitmap CacheImageSmall(int pictureRef)
    {
      Bitmap image = this.LoadImage(pictureRef);
      if (image != null)
      {
        image = GraphicsHelper.ScaleLimitImage(image, this._ImageSmallSize, this._ImageSmallSize, 1f);
        lock (this._LockObject)
          this._ImagesSmall[(object) pictureRef] = (object) image;
      }
      return image;
    }

    private Bitmap CacheImage(Habitat habitat)
    {
      Bitmap bitmap = (Bitmap) null;
      if (habitat != null)
        bitmap = this.CacheImage((int) habitat.PictureRef);
      return bitmap;
    }

    private Bitmap CacheImage(int pictureRef)
    {
      Bitmap bitmap = this.LoadImage(pictureRef);
      if (bitmap != null)
      {
        lock (this._LockObject)
        {
          this._Images[(object) pictureRef] = (object) bitmap;
          this._LastUsage[(object) pictureRef] = (object) DateTime.Now;
        }
      }
      return bitmap;
    }

    private void LoadImagesSmall()
    {
      this.ClearImageCache(this._ImagesSmall);
      for (int index = 0; index < this._Filepaths.Count; ++index)
      {
        Bitmap image = this.LoadImage(this._Filepaths[index]);
        Bitmap bitmap = GraphicsHelper.ScaleLimitImage(image, this._ImageSmallSize, this._ImageSmallSize, 1f);
        lock (this._LockObject)
          this._ImagesSmall.Add((object) index, (object) bitmap);
        if (image != null && image.PixelFormat != PixelFormat.Undefined)
          image.Dispose();
      }
    }

    public string ResolveImageFilename(Habitat habitat)
    {
      string str = string.Empty;
      if (habitat != null)
        str = this.ResolveImageFilename((int) habitat.PictureRef);
      return str;
    }

    public string ResolveImageFilename(int pictureRef)
    {
      string str = string.Empty;
      if (pictureRef >= 0 && pictureRef < this._Filepaths.Count)
        str = this._Filepaths[pictureRef];
      return str;
    }

    private Bitmap LoadImage(Habitat habitat)
    {
      Bitmap bitmap = (Bitmap) null;
      if (habitat != null && habitat.PictureRef >= (short) 0)
        bitmap = this.LoadImage((int) habitat.PictureRef);
      return bitmap;
    }

    private Bitmap LoadImage(int pictureRef) => this.LoadImage(this.ResolveImageFilename(pictureRef));

    private Bitmap LoadImage(string filepath)
    {
      Bitmap bitmap = (Bitmap) null;
      if (File.Exists(filepath))
        bitmap = this.SafeLoadImage(filepath);
      return bitmap;
    }

    private Bitmap SafeLoadImage(string imagePath)
    {
      Bitmap bitmap = (Bitmap) null;
      if (File.Exists(imagePath))
      {
        try
        {
          bitmap = GraphicsHelper.LoadImageFromFilePath(imagePath);
        }
        catch (Exception ex)
        {
          bitmap = (Bitmap) null;
        }
      }
      return bitmap;
    }

    private void GenerateHabitatImageFilepaths(bool initialLoad, out bool hasCustom)
    {
      string str1 = this._ApplicationStartupPath + "\\images\\";
      string str2 = str1 + "environment\\planets";
      string str3 = str1 + "environment\\asteroids";
      string path1 = string.Empty;
      string path2 = string.Empty;
      if (!string.IsNullOrEmpty(this._CustomizationSetName))
      {
        path1 = this._ApplicationStartupPath + "\\customization\\" + this._CustomizationSetName + "\\images\\environment\\planets";
        if (!Directory.Exists(path1))
          path1 = string.Empty;
        path2 = this._ApplicationStartupPath + "\\customization\\" + this._CustomizationSetName + "\\images\\environment\\asteroids";
        if (!Directory.Exists(path2))
          path2 = string.Empty;
      }
      hasCustom = false;
      this._Filepaths.Clear();
      for (int index = 0; index < GalaxyImages.HabitatImageCountBarrenRock; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("Barren-" + (index + 1).ToString("0000") + ".png", str2 + "\\barrenrock\\", path1 + "\\barrenrock\\", ref hasCustom));
      for (int index = 0; index < GalaxyImages.HabitatImageCountContinental; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("Continental-" + (index + 1).ToString("0000") + ".png", str2 + "\\continental\\", path1 + "\\continental\\", ref hasCustom));
      for (int index = 0; index < GalaxyImages.HabitatImageCountForest; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("Forest-" + (index + 1).ToString("0000") + ".png", str2 + "\\forest\\", path1 + "\\forest\\", ref hasCustom));
      for (int index = 0; index < GalaxyImages.HabitatImageCountFrozenGasGiantArgon; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("FrozenGasAr-" + (index + 1).ToString("0000") + ".png", str2 + "\\frozengasgiant\\", path1 + "\\frozengasgiant\\", ref hasCustom));
      for (int index = 0; index < GalaxyImages.HabitatImageCountFrozenGasGiantHelium; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("FrozenGasHe-" + (index + 1).ToString("0000") + ".png", str2 + "\\frozengasgiant\\", path1 + "\\frozengasgiant\\", ref hasCustom));
      for (int index = 0; index < GalaxyImages.HabitatImageCountFrozenGasGiantKrypton; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("FrozenGasKr-" + (index + 1).ToString("0000") + ".png", str2 + "\\frozengasgiant\\", path1 + "\\frozengasgiant\\", ref hasCustom));
      for (int index = 0; index < GalaxyImages.HabitatImageCountFrozenGasGiantTyderios; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("FrozenGasTy-" + (index + 1).ToString("0000") + ".png", str2 + "\\frozengasgiant\\", path1 + "\\frozengasgiant\\", ref hasCustom));
      for (int index = 0; index < GalaxyImages.HabitatImageCountFrozenGasGiantAny; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("FrozenGasOt-" + (index + 1).ToString("0000") + ".png", str2 + "\\frozengasgiant\\", path1 + "\\frozengasgiant\\", ref hasCustom));
      for (int index = 0; index < GalaxyImages.HabitatImageCountGasGiantArgon; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("GasGiantAr-" + (index + 1).ToString("0000") + ".png", str2 + "\\gasgiant\\", path1 + "\\gasgiant\\", ref hasCustom));
      for (int index = 0; index < GalaxyImages.HabitatImageCountGasGiantCaslon; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("GasGiantCa-" + (index + 1).ToString("0000") + ".png", str2 + "\\gasgiant\\", path1 + "\\gasgiant\\", ref hasCustom));
      for (int index = 0; index < GalaxyImages.HabitatImageCountGasGiantHelium; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("GasGiantHe-" + (index + 1).ToString("0000") + ".png", str2 + "\\gasgiant\\", path1 + "\\gasgiant\\", ref hasCustom));
      for (int index = 0; index < GalaxyImages.HabitatImageCountGasGiantHydrogen; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("GasGiantHy-" + (index + 1).ToString("0000") + ".png", str2 + "\\gasgiant\\", path1 + "\\gasgiant\\", ref hasCustom));
      for (int index = 0; index < GalaxyImages.HabitatImageCountGasGiantKrypton; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("GasGiantKr-" + (index + 1).ToString("0000") + ".png", str2 + "\\gasgiant\\", path1 + "\\gasgiant\\", ref hasCustom));
      for (int index = 0; index < GalaxyImages.HabitatImageCountGasGiantAny; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("GasGiantOt-" + (index + 1).ToString("0000") + ".png", str2 + "\\gasgiant\\", path1 + "\\gasgiant\\", ref hasCustom));
      for (int index = 0; index < GalaxyImages.HabitatImageCountIce; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("Glacial-" + (index + 1).ToString("0000") + ".png", str2 + "\\iceglacial\\", path1 + "\\iceglacial\\", ref hasCustom));
      for (int index = 0; index < GalaxyImages.HabitatImageCountMarshySwamp; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("Marsh-" + (index + 1).ToString("0000") + ".png", str2 + "\\marshyswamp\\", path1 + "\\marshyswamp\\", ref hasCustom));
      for (int index = 0; index < GalaxyImages.HabitatImageCountOcean; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("Ocean-" + (index + 1).ToString("0000") + ".png", str2 + "\\ocean\\", path1 + "\\ocean\\", ref hasCustom));
      for (int index = 0; index < GalaxyImages.HabitatImageCountDesert; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("Desert-" + (index + 1).ToString("0000") + ".png", str2 + "\\sandydesert\\", path1 + "\\sandydesert\\", ref hasCustom));
      for (int index = 0; index < GalaxyImages.HabitatImageCountVolcanic; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("Volcanic-" + (index + 1).ToString("0000") + ".png", str2 + "\\volcanic\\", path1 + "\\volcanic\\", ref hasCustom));
      for (int index = 0; index < GalaxyImages.HabitatImageCountAsteroidsNormal; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("AstRck-" + (index + 1).ToString("0000") + ".png", str3 + "\\rocky\\", path2 + "\\rocky\\", ref hasCustom));
      for (int index = 0; index < GalaxyImages.HabitatImageCountAsteroidsIce; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("AstIce-" + (index + 1).ToString("0000") + ".png", str3 + "\\ice\\", path2 + "\\ice\\", ref hasCustom));
      for (int index = 0; index < GalaxyImages.HabitatImageCountAsteroidsMetal; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("AstMtl-" + (index + 1).ToString("0000") + ".png", str3 + "\\metal\\", path2 + "\\metal\\", ref hasCustom));
      for (int index = 0; index < GalaxyImages.HabitatImageCountAsteroidsGold; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("AstGold-" + (index + 1).ToString("0000") + ".png", str3 + "\\metal\\", path2 + "\\metal\\", ref hasCustom));
      for (int index = 0; index < GalaxyImages.HabitatImageCountAsteroidsCrystal; ++index)
        this._Filepaths.Add(this.GetFilePathForImage("AstCryst-" + (index + 1).ToString("0000") + ".png", str3 + "\\metal\\", path2 + "\\metal\\", ref hasCustom));
      if (Directory.Exists(path1 + "\\other\\"))
      {
        string[] files = Directory.GetFiles(path1 + "\\other\\", "*.png");
        if (files.Length > 0)
        {
          for (int index = 0; index < files.Length; ++index)
            this._Filepaths.Add(files[index]);
          hasCustom = true;
        }
      }
      this._IsCustomSet = hasCustom;
    }

    public Bitmap[] GetAsteroidImages()
    {
      int num1 = 0;
      int num2 = 0;
      Bitmap[] asteroidImages = new Bitmap[num2 - num1];
      for (int pictureRef = num1; pictureRef < num2; ++pictureRef)
        asteroidImages[pictureRef] = this.LoadImage(pictureRef);
      return asteroidImages;
    }

    private string GetFilePathForImage(
      string filename,
      string basePath,
      string customPath,
      ref bool isCustom)
    {
      string empty = string.Empty;
      string filePathForImage;
      if (!string.IsNullOrEmpty(customPath) && File.Exists(customPath + filename))
      {
        filePathForImage = customPath + filename;
        isCustom = true;
      }
      else
        filePathForImage = basePath + filename;
      return filePathForImage;
    }

    private void ClearImageCache(Hashtable images)
    {
      lock (this._LockObject)
      {
        foreach (int key in (IEnumerable) images.Keys)
        {
          object image = images[(object) key];
          if (image != null && image is Bitmap)
            ((Image) image).Dispose();
        }
        images.Clear();
      }
    }

    public void ClearCache() => this.ClearImageCache(this._Images);
  }
}