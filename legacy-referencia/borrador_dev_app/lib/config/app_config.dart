import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:shared_preferences/shared_preferences.dart';

class AppConfig {
  static const _kBaseUrlKey = 'api_base_url';

  static String _defaultBaseUrl() =>
      dotenv.env['API_BASE_URL'] ?? 'http://10.0.2.2:5001';

  static Future<String> getBaseUrl() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_kBaseUrlKey) ?? _defaultBaseUrl();
  }

  static Future<void> setBaseUrl(String url) async {
    final prefs = await SharedPreferences.getInstance();
    final cleaned = url.trim().replaceAll(RegExp(r'\/+$'), '');
    await prefs.setString(_kBaseUrlKey, cleaned);
  }
}
