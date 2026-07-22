import 'dart:convert';
import 'dart:async';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:borrador_dev_app/config/app_config.dart';

class ParametrosScreen extends StatefulWidget {
  const ParametrosScreen({super.key});

  @override
  State<ParametrosScreen> createState() => _ParametrosScreenState();
}

class _ParametrosScreenState extends State<ParametrosScreen> {
  final ctrl = TextEditingController();

  String? errorMsg;      // Mensaje de error visible
  String? okMsg;         // Mensaje OK visible
  bool loading = true;
  bool testing = false;

  String? _lastTestedUrl; // Para bloquear "Guardar" si cambió después del test

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    ctrl.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    final url = await AppConfig.getBaseUrl();
    ctrl.text = url;

    if (!mounted) return;
    setState(() => loading = false);
  }

  String _cleanUrl(String s) => s.trim().replaceAll(RegExp(r'\/+$'), '');

  bool _isValidUrl(String s) {
    final v = _cleanUrl(s);
    final uri = Uri.tryParse(v);
    if (uri == null) return false;

    // Requerimos http/https y host válido
    if (uri.scheme != 'http' && uri.scheme != 'https') return false;
    if (uri.host.isEmpty) return false;

    return true;
  }

  Future<void> _testConexion() async {
    setState(() {
      testing = true;
      errorMsg = null;
      okMsg = null;
    });

    final baseUrl = _cleanUrl(ctrl.text);

    if (!_isValidUrl(baseUrl)) {
      if (!mounted) return;
      setState(() {
        testing = false;
        errorMsg = 'Conexi\u00F3n fallida: URL inv\u00E1lida';
      });
      return;
    }

    try {
      final uri = Uri.parse('$baseUrl/borrador-dev/run');

      final res = await http
          .post(
            uri,
            headers: {'Content-Type': 'application/json'},
            body: jsonEncode({"numero": 0, "aprobar": false}),
          )
          .timeout(const Duration(seconds: 5));

      if (!mounted) return;

      if (res.statusCode >= 200 && res.statusCode < 300) {
        setState(() {
          okMsg = 'Conexi\u00F3n OK';
          errorMsg = null;
          _lastTestedUrl = baseUrl;
        });
      } else {
        setState(() {
          errorMsg = 'Conexi\u00F3n fallida: HTTP ${res.statusCode}';
          okMsg = null;
          _lastTestedUrl = null;
        });
      }
    } on TimeoutException {
      if (!mounted) return;
      setState(() {
        errorMsg = 'Conexi\u00F3n fallida: tiempo de espera';
        okMsg = null;
        _lastTestedUrl = null;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        errorMsg = 'Conexi\u00F3n fallida';
        okMsg = null;
        _lastTestedUrl = null;
      });
    } finally {
      if (!mounted) return;
      setState(() => testing = false);
    }
  }

  Future<void> _save() async {
    final v = _cleanUrl(ctrl.text);

    if (!_isValidUrl(v)) {
      setState(() => errorMsg = 'Conexi\u00F3n fallida: URL inv\u00E1lida');
      return;
    }

    // Si cambió el URL desde el último test OK, obliga a testear de nuevo
    if (_lastTestedUrl == null || _lastTestedUrl != v) {
      setState(() => errorMsg = 'Primero prueba la conexi\u00F3n con este URL');
      return;
    }

    await AppConfig.setBaseUrl(v);
    if (!mounted) return;
    Navigator.pop(context, true);
  }

  @override
  Widget build(BuildContext context) {
    final cleaned = _cleanUrl(ctrl.text);
    final canSave =
        !testing &&
        okMsg == 'Conexi\u00F3n OK' &&
        _lastTestedUrl != null &&
        _lastTestedUrl == cleaned;

    return Scaffold(
      appBar: AppBar(title: const Text('Par\u00E1metros')),
      body: Padding(
        padding: const EdgeInsets.all(12),
        child: loading
            ? const Center(child: CircularProgressIndicator())
            : Column(
                children: [
                  TextField(
                    controller: ctrl,
                    keyboardType: TextInputType.url,
                    decoration: const InputDecoration(
                      labelText: 'API Base URL',
                      hintText: 'http://192.168.0.117:5001',
                      border: OutlineInputBorder(),
                      isDense: true,
                      contentPadding:
                          EdgeInsets.symmetric(horizontal: 10, vertical: 10),
                    ),
                    onChanged: (_) {
                      setState(() {
                        okMsg = null;
                        errorMsg = null;
                        _lastTestedUrl = null; // forzar nuevo test
                      });
                    },
                  ),
                  const SizedBox(height: 10),

                  Row(
                    children: [
                      Expanded(
                        child: ElevatedButton(
                          onPressed: testing ? null : _testConexion,
                          child: Text(testing
                              ? 'Probando...'
                              : 'Probar conexi\u00F3n'),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: ElevatedButton(
                          onPressed: canSave ? _save : null,
                          child: const Text('Guardar'),
                        ),
                      ),
                    ],
                  ),

                  const SizedBox(height: 10),

                  if (okMsg != null)
                    Container(
                      width: double.infinity,
                      padding: const EdgeInsets.all(10),
                      color: Colors.green.withOpacity(0.12),
                      child: Text(
                        okMsg!,
                        style: const TextStyle(color: Colors.green),
                      ),
                    ),

                  if (errorMsg != null)
                    Container(
                      width: double.infinity,
                      padding: const EdgeInsets.all(10),
                      color: Colors.red.withOpacity(0.12),
                      child: Text(
                        errorMsg!,
                        style: const TextStyle(color: Colors.red),
                      ),
                    ),
                ],
              ),
      ),
    );
  }
}

