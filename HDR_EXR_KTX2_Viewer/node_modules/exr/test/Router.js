import test from 'ava'

var path = require('path')
var methods = require('methods')
var after = require('after');
var assert = require('assert');
var Router = require('../lib/router/')

test("should return a function with router methods", t => {
  var router = Router();
  assert(typeof router == 'function');

  var router = new Router();
  assert(typeof router == 'function');

  assert(typeof router.get == 'function');
  assert(typeof router.handle == 'function');
  assert(typeof router.use == 'function');
});

test.cb('should support .use of other routers',t => {
  var router = new Router();
  var another = new Router();

  another.get('/bar', function(req, res){
    res.end();
  });
  router.use('/foo', another);

  router.handle({ url: '/foo/bar', method: 'GET' }, { end: t.end });
});

test.cb('should support dynamic routes', t => {
  var router = new Router();
  var another = new Router();

  another.get('/:bar', function(req, res){
    t.is(req.params.bar, 'route')
    res.end();
  });
  router.use('/:foo', another);

  router.handle({ url: '/test/route', method: 'GET' }, { end: t.end });
});

test.cb('should handle blank URL', t => {
  var router = new Router();

  router.use(function (req, res) {
    // false.should.be.true;
    t.fail()
  });

  router.handle({ url: '', method: 'GET' }, {}, t.end);
});

test.cb('should not stack overflow with many registered routes', t => {
  var handler = function(req, res){ res.end(new Error('wrong handler')) };
  var router = new Router();

  for (var i = 0; i < 6000; i++) {
    router.get('/thing' + i, handler)
  }

  router.get('/', function (req, res) {
    res.end();
  });

  router.handle({ url: '/', method: 'GET' }, { end: t.end });
});


// '.handle'
test.cb('.handle should dispatch', t => {
  var router = new Router();

  router.route('/foo').get(function(req, res){
    res.send('foo');
  });

  var res = {
    send: function(val) {
      // val.should.equal('foo');
      t.is(val, 'foo')
      t.end();
    }
  }
  router.handle({ url: '/foo', method: 'GET' }, res);
})

// .multiple callbacks
test('.multiple callbacks should throw if a callback is null', t => {
  assert.throws(function () {
    var router = new Router();
    router.route('/foo').all(null);
  })
})

test('.multiple callbacks should throw if a callback is undefined', t => {
  assert.throws(function () {
    var router = new Router();
    router.route('/foo').all(undefined);
  })
})

test('.multiple callbacks should throw if a callback is not a function', t => {
  assert.throws(function () {
    var router = new Router();
    router.route('/foo').all('not a function');
  })
})

test('.multiple callbacks should not throw if all callbacks are functions', t => {
  var router = new Router();
  router.route('/foo').all(function(){}).all(function(){});
})

// 'error'
test.cb('should skip non error middleware', t => {
  var router = new Router();

  router.get('/foo', function(req, res, next){
    next(new Error('foo'));
  });

  router.get('/bar', function(req, res, next){
    next(new Error('bar'));
  });

  router.use(function(req, res, next){
    assert(false);
  });

  router.use(function(err, req, res, next){
    assert.equal(err.message, 'foo');
    t.end();
  });

  router.handle({ url: '/foo', method: 'GET' }, {}, t.end);
});

test.cb('should handle throwing inside routes with params', t => {
  var router = new Router();

  router.get('/foo/:id', function(req, res, next){
    throw new Error('foo');
  });

  router.use(function(req, res, next){
    assert(false);
  });

  router.use(function(err, req, res, next){
    assert.equal(err.message, 'foo');
    t.end();
  });

  router.handle({ url: '/foo/2', method: 'GET' }, {}, function() {});
});

test.cb('should handle throwing in handler after async param', t => {
  var router = new Router();

  router.param('user', function(req, res, next, val){
    process.nextTick(function(){
      req.user = val;
      next();
    });
  });

  router.use('/:user', function(req, res, next){
    throw new Error('oh no!');
  });

  router.use(function(err, req, res, next){
    assert.equal(err.message, 'oh no!');
    t.end();
  });

  router.handle({ url: '/bob', method: 'GET' }, {}, function() {});
});

test.cb('should handle throwing inside error handlers', t => {
  var router = new Router();

  router.use(function(req, res, next){
    throw new Error('boom!');
  });

  router.use(function(err, req, res, next){
    throw new Error('oops');
  });

  router.use(function(err, req, res, next){
    assert.equal(err.message, 'oops');
    t.end();
  });

  router.handle({ url: '/', method: 'GET' }, {}, t.end);
});

// FQDN
test.cb('FQDN should not obscure FQDNs', t => {
  var request = { hit: 0, url: 'http://example.com/foo', method: 'GET' };
  var router = new Router();

  router.use(function (req, res, next) {
    assert.equal(req.hit++, 0);
    assert.equal(req.url, 'http://example.com/foo');
    next();
  });

  router.handle(request, {}, function (err) {
    t.ifError(err)
    assert.equal(request.hit, 1);
    t.end();
  });
});

test.cb('FQDN should ignore FQDN in search', t => {
  var request = { hit: 0, url: '/proxy?url=http://example.com/blog/post/1', method: 'GET' };
  var router = new Router();

  router.use('/proxy', function (req, res, next) {
    assert.equal(req.hit++, 0);
    assert.equal(req.url, '/?url=http://example.com/blog/post/1');
    next();
  });

  router.handle(request, {}, function (err) {
    t.ifError(err)
    assert.equal(request.hit, 1);
    t.end();
  });
});

test.cb('FQDN should ignore FQDN in path', t => {
  var request = { hit: 0, url: '/proxy/http://example.com/blog/post/1', method: 'GET' };
  var router = new Router();

  router.use('/proxy', function (req, res, next) {
    assert.equal(req.hit++, 0);
    assert.equal(req.url, '/http://example.com/blog/post/1');
    next();
  });

  router.handle(request, {}, function (err) {
    t.ifError(err)
    assert.equal(request.hit, 1);
    t.end();
  });
});

test.cb('FQDN should adjust FQDN req.url', t => {
  var request = { hit: 0, url: 'http://example.com/blog/post/1', method: 'GET' };
  var router = new Router();

  router.use('/blog', function (req, res, next) {
    assert.equal(req.hit++, 0);
    assert.equal(req.url, 'http://example.com/post/1');
    next();
  });

  router.handle(request, {}, function (err) {
    t.ifError(err)
    assert.equal(request.hit, 1);
    t.end();
  });
});

test.cb('FQDN should adjust FQDN req.url with multiple handlers', t => {
  var request = { hit: 0, url: 'http://example.com/blog/post/1', method: 'GET' };
  var router = new Router();

  router.use(function (req, res, next) {
    assert.equal(req.hit++, 0);
    assert.equal(req.url, 'http://example.com/blog/post/1');
    next();
  });

  router.use('/blog', function (req, res, next) {
    assert.equal(req.hit++, 1);
    assert.equal(req.url, 'http://example.com/post/1');
    next();
  });

  router.handle(request, {}, function (err) {
    t.ifError(err)
    assert.equal(request.hit, 2);
    t.end();
  });
});

test.cb('FQDN should adjust FQDN req.url with multiple routed handlers', t => {
  var request = { hit: 0, url: 'http://example.com/blog/post/1', method: 'GET' };
  var router = new Router();

  router.use('/blog', function (req, res, next) {
    assert.equal(req.hit++, 0);
    assert.equal(req.url, 'http://example.com/post/1');
    next();
  });

  router.use('/blog', function (req, res, next) {
    assert.equal(req.hit++, 1);
    assert.equal(req.url, 'http://example.com/post/1');
    next();
  });

  router.use(function (req, res, next) {
    assert.equal(req.hit++, 2);
    assert.equal(req.url, 'http://example.com/blog/post/1');
    next();
  });

  router.handle(request, {}, function (err) {
    t.ifError(err)
    assert.equal(request.hit, 3);
    t.end();
  });
})

// .all
test.cb('.all should support using .all to capture all http verbs', t => {
  var router = new Router();

  var count = 0;
  router.all('/foo', function(){ count++; });

  var url = '/foo?bar=baz';

  methods.forEach(function testMethod(method) {
    router.handle({ url: url, method: method }, {}, function() {});
  });

  assert.equal(count, methods.length);
  t.end();
})

// describe('.use', function() {
test('.use should require arguments', t => {
  var router = new Router();
  try {
    router.use.bind(router)
  }catch(err) {
    t.throws(/requires middleware function/)
  }
  // router.use.bind(router).should.throw(/requires middleware function/)
})

test('.use should not accept non-functions', t => {
  var router = new Router();
  try {
    router.use.bind(router, '/', 'hello')
  }catch(err) {
    t.throws(/requires middleware function.*string/)
  }
  
  try {
    router.use.bind(router, '/', 5)
  }catch(err) {
    t.throws(/requires middleware function.*number/)
  }
  
  try {
    router.use.bind(router, '/', null)
  }catch(err) {
    t.throws(/requires middleware function.*Null/)
  }
  
  try {
    router.use.bind(router, '/', new Date())
  }catch(err) {
    t.throws(/requires middleware function.*Date/)
  }
})

test('.use should accept array of middleware', t => {
  var count = 0;
  var router = new Router();

  function fn1(req, res, next){
    assert.equal(++count, 1);
    next();
  }

  function fn2(req, res, next){
    assert.equal(++count, 2);
    next();
  }

  router.use([fn1, fn2], function(req, res){
    assert.equal(++count, 3);
    t.end();
  });

  router.handle({ url: '/foo', method: 'GET' }, {}, function(){});
})

//.param 
test.cb('.param should call param function when routing VERBS', t => {
  var router = new Router();

  router.param('id', function(req, res, next, id) {
    assert.equal(id, '123');
    next();
  });

  router.get('/foo/:id/bar', function(req, res, next) {
    assert.equal(req.params.id, '123');
    next();
  });

  router.handle({ url: '/foo/123/bar', method: 'get' }, {}, t.end);
});

test.cb('.param should call param function when routing middleware', t => {
  var router = new Router();

  router.param('id', function(req, res, next, id) {
    assert.equal(id, '123');
    next();
  });

  router.use('/foo/:id/bar', function(req, res, next) {
    assert.equal(req.params.id, '123');
    assert.equal(req.url, '/baz');
    next();
  });

  router.handle({ url: '/foo/123/bar/baz', method: 'get' }, {}, t.end);
});

test.cb('.param should only call once per request', t => {
  var count = 0;
  var req = { url: '/foo/bob/bar', method: 'get' };
  var router = new Router();
  var sub = new Router();

  sub.get('/bar', function(req, res, next) {
    next();
  });

  router.param('user', function(req, res, next, user) {
    count++;
    req.user = user;
    next();
  });

  router.use('/foo/:user/', new Router());
  router.use('/foo/:user/', sub);

  router.handle(req, {}, function(err) {
    t.ifError(err)
    assert.equal(count, 1);
    assert.equal(req.user, 'bob');
    t.end();
  });
});

test.cb('.param should call when values differ', t => {
  var count = 0;
  var req = { url: '/foo/bob/bar', method: 'get' };
  var router = new Router();
  var sub = new Router();

  sub.get('/bar', function(req, res, next) {
    next();
  });

  router.param('user', function(req, res, next, user) {
    count++;
    req.user = user;
    next();
  });

  router.use('/foo/:user/', new Router());
  router.use('/:user/bob/', sub);

  router.handle(req, {}, function(err) {
    t.ifError(err)
    assert.equal(count, 2);
    assert.equal(req.user, 'foo');
    t.end();
  });
});

// parallel requests
test.cb('parallel requests should not mix requests', t => {
  var req1 = { url: '/foo/50/bar', method: 'get' };
  var req2 = { url: '/foo/10/bar', method: 'get' };
  var router = new Router();
  var sub = new Router();

  var done = after(2, t.end);

  sub.get('/bar', function(req, res, next) {
    next();
  });

  sub.get('/bar', function(req, res, next) {
    next();
  });

  router.param('ms', function(req, res, next, ms) {
    ms = parseInt(ms, 10);
    req.ms = ms;
    setTimeout(next, ms);
  });

  router.use('/foo/:ms/', new Router());
  router.use('/foo/:ms/', sub);

  router.handle(req1, {}, function(err) {
    assert.ifError(err);
    assert.equal(req1.ms, 50);
    assert.equal(req1.originalUrl, '/foo/50/bar');
    done();
  });

  router.handle(req2, {}, function(err) {
    assert.ifError(err);
    assert.equal(req2.ms, 10);
    assert.equal(req2.originalUrl, '/foo/10/bar');
    done();
  });
});
