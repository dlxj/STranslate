﻿using System.Globalization;
using System.Windows.Data;
using STranslate.Model;

namespace STranslate.Style.Converters;

public class ServiceType2StringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // 提示词 ContextMenu 是否显示
        if (parameter is "prompt" && value is ITranslator service) return service is ITranslatorLlm ? "1" : "0";

        // 服务类型转换为字符串以显示
        return value switch
        {
            ServiceType sType => sType switch
            {
                ServiceType.DeepLXService => "自建",
                ServiceType.GoogleBuiltinService => "内置",
                ServiceType.YandexBuiltInService => "内置",
                ServiceType.STranslateService => "内置",
                ServiceType.EcdictService => "内置",
                ServiceType.KingSoftDictService => "内置",
                ServiceType.BingDictService => "内置",
                ServiceType.MicrosoftBuiltinService => "内置",
                _ => "官方"
                //TODO: 新接口需要适配
            },
            OCRType oType => oType switch
            {
                OCRType.PaddleOCR => "内置",
                OCRType.WeChatOCR => "内置",
                _ => "官方"
                //TODO: 新TTS服务需要适配
            },
            TTSType tType => tType switch
            {
                TTSType.OfflineTTS => "内置",
                TTSType.EdgeTTS => "内置",
                TTSType.LingvaTTS => "自建",
                _ => "官方"
                //TODO: 新OCR服务需要适配
            },
            VocabularyBookType vType => vType switch
            {
                _ => "官方"
            },
            _ => "自建"
            //TODO: 新生词本服务需要适配
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}