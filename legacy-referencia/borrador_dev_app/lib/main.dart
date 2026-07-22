import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';

import 'package:borrador_dev_app/config/app_config.dart';
import 'package:borrador_dev_app/screen/borrador_form_screen.dart';
import 'package:borrador_dev_app/screen/parametros_screen.dart';
import 'package:borrador_dev_app/services/borrador_dev_api.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await dotenv.load(fileName: ".env"); // opcional: default baseUrl
  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'Borrador Dev',
      theme: ThemeData(useMaterial3: true),
      home: const AppRoot(),
    );
  }
}

class AppRoot extends StatefulWidget {
  const AppRoot({super.key});

  @override
  State<AppRoot> createState() => _AppRootState();
}

class _AppRootState extends State<AppRoot> {
  String? baseUrl;

  @override
  void initState() {
    super.initState();
    _loadBaseUrl();
  }

  Future<void> _loadBaseUrl() async {
    final url = await AppConfig.getBaseUrl();
    setState(() => baseUrl = url);
  }

  Future<void> _openParametros() async {
    final changed = await Navigator.of(context).push<bool>(
      MaterialPageRoute(builder: (_) => const ParametrosScreen()),
    );

    if (changed == true) {
      await _loadBaseUrl(); // recarga la URL guardada
    }
  }

  @override
  Widget build(BuildContext context) {
    if (baseUrl == null) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }

    final api = BorradorDevApi(baseUrl!);

    return BorradorFormScreen(
      api: api,
      baseUrl: baseUrl!,
      onOpenParametros: _openParametros,
    );
  }
}
